using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Characters;

namespace RedRunner.TerrainGeneration
{

	public abstract class TerrainGenerator : MonoBehaviour
	{

		private static TerrainGenerator m_Singleton;

		public static TerrainGenerator Singleton
		{
			get
			{
				return m_Singleton;
			}
		}

		protected Dictionary<Vector3, Block> m_Blocks;
		protected Dictionary<Vector3, BackgroundBlock> m_BackgroundBlocks;
		protected BackgroundLayer[] m_BackgroundLayers;
		protected float m_PreviousX;
		protected float m_CurrentX;
		protected float m_FathestBackgroundX;
		[SerializeField]
		protected TerrainGenerationSettings m_Settings;
		protected int m_GeneratedStartBlocksCount;
		protected int m_GeneratedMiddleBlocksCount;
		protected int m_GeneratedEndBlocksCount;
		[SerializeField]
		protected float m_DestroyRange = 100f;
		[SerializeField]
		protected float m_GenerateRange = 100f;
		[SerializeField]
		protected float m_BackgroundGenerateRange = 200f;
		[SerializeField]
		protected Character m_Character;
		protected Block m_LastBlock;
		protected BackgroundBlock m_LastBackgroundBlock;
		protected float m_RemoveTime = 0f;
		protected bool m_Reset = false;
		protected int m_CurrentBlockIndex = 0;
		protected Dictionary<string, Block> m_BlockPrefabCache = new Dictionary<string, Block>();
		public bool IsPreLoaded { get; private set; }


		public float PreviousX
		{
			get
			{
				return m_PreviousX;
			}
		}

		public float CurrentX
		{
			get
			{
				return m_CurrentX;
			}
		}

		public TerrainGenerationSettings Settings
		{
			get
			{
				return m_Settings;
			}
		}

		protected virtual void Awake ()
		{
			if ( m_Singleton != null )
			{
				Destroy ( gameObject );
				return;
			}
			m_Singleton = this;
			m_Blocks = new Dictionary<Vector3, Block> ();
			m_BackgroundBlocks = new Dictionary<Vector3, BackgroundBlock> ();

			// NULL CHECK: Prevent crash if settings or background layers are missing
			if ( m_Settings != null && m_Settings.BackgroundLayers != null )
			{
				m_BackgroundLayers = new BackgroundLayer[m_Settings.BackgroundLayers.Length];
				for ( int i = 0; i < m_Settings.BackgroundLayers.Length; i++ )
				{
					m_BackgroundLayers [ i ] = m_Settings.BackgroundLayers [ i ];
				}
			}
			else
			{
				// Initialize empty array if settings missing (prevents null reference)
				m_BackgroundLayers = new BackgroundLayer[0];
				Debug.LogWarning("[TerrainGenerator] TerrainGenerationSettings or BackgroundLayers is null - no background will be generated");
			}

			GameManager.OnReset += Reset;

			// Pre-load all block prefabs spread across frames to prevent WebGL freeze
			StartCoroutine ( PreLoadAllBlockPrefabsAsync () );
		}

		private IEnumerator PreLoadAllBlockPrefabsAsync ()
		{
			// Warm up all compiled shader variants to prevent first-use compilation stutter
			Shader.WarmupAllShaders ();
			yield return null;

			// Load block prefabs one per frame so WebGL decompression doesn't freeze the browser
			GetBlockPrefab ( "Start" );
			yield return null;
			GetBlockPrefab ( "Middle" );
			yield return null;

			for ( int i = 1; i <= 31; i++ )
			{
				GetBlockPrefab ( "Middle_" + i );
				yield return null;
			}

			IsPreLoaded = true;
		}

		protected Block GetBlockPrefab ( string blockName )
		{
			// Lazy-load and cache: only load each block once, on first use
			if ( m_BlockPrefabCache.TryGetValue ( blockName, out Block cached ) )
				return cached;
			var block = Resources.Load<Block> ( "Blocks/" + blockName );
			if ( block != null )
				m_BlockPrefabCache [ blockName ] = block;
			return block;
		}

		protected virtual void Reset ()
		{
			m_Reset = true;
			RemoveAll ();
			m_CurrentX = 0f;
			m_LastBlock = null;
			m_LastBackgroundBlock = null;
			if (m_BackgroundLayers != null)
			{
				for ( int i = 0; i < m_BackgroundLayers.Length; i++ )
				{
					m_BackgroundLayers [ i ].Reset ();
				}
			}
			m_FathestBackgroundX = 0f;
			m_Blocks.Clear ();
			m_BackgroundBlocks.Clear ();
			m_GeneratedStartBlocksCount = 0;
			m_GeneratedMiddleBlocksCount = 0;
			m_GeneratedEndBlocksCount = 0;
			m_CurrentBlockIndex = 0;
			m_Reset = false;
		}

		protected virtual void OnDestroy ()
		{
			m_Singleton = null;
		}

		protected virtual void Update ()
		{
			if ( m_Reset )
			{
				return;
			}
			if ( m_RemoveTime < Time.time )
			{
				m_RemoveTime = Time.time + 5f;
				Remove ();
			}
			Generate ();
		}

		public virtual void Generate ()
		{
			// CHANGED TO 100 TO ALLOW MIDDLE_32, MIDDLE_33, ETC. DO NOT REVERT TO 32.
			if ( m_CurrentBlockIndex > 100 )
			{
				return;
			}
			
			Block blockPrefab = null;
			string blockName = "";

			if ( m_CurrentBlockIndex == 0 )
			{
				blockName = "Start";
			}
			else if ( m_CurrentBlockIndex == 1 )
			{
				blockName = "Middle";
			}
			else
			{
				blockName = "Middle_" + ( m_CurrentBlockIndex - 1 );
			}

			blockPrefab = GetBlockPrefab ( blockName );

			if ( blockPrefab != null )
			{
				Vector3 current = new Vector3 ( m_CurrentX, 0f, 0f );
				CreateBlock ( blockPrefab, current );
				m_CurrentBlockIndex++;
			}
			
			// Background generation (only if layers exist)
		if ( m_BackgroundLayers != null && m_BackgroundLayers.Length > 0 )
		{
			for ( int i = 0; i < m_BackgroundLayers.Length; i++ )
			{
				int random = Random.Range ( 0, 2 );
				bool generate = random == 1 ? true : false;
				if ( !generate )
				{
					continue;
				}
				Vector3 current = new Vector3 ( m_BackgroundLayers [ i ].CurrentX, 0f, 0f );
				BackgroundBlock block = ( BackgroundBlock )ChooseFrom ( m_BackgroundLayers [ i ].Blocks );
				float newX = 0f;
				if ( m_BackgroundLayers [ i ].LastBlock != null )
				{
					newX = m_BackgroundLayers [ i ].CurrentX + m_BackgroundLayers [ i ].LastBlock.Width;
				}
				else
				{
					newX = 0f;
				}
				if ( block != null && ( m_BackgroundLayers [ i ].LastBlock == null || newX < m_Character.transform.position.x + m_BackgroundGenerateRange ) )
				{
					CreateBackgroundBlock ( block, current, m_BackgroundLayers [ i ], i );
				}
			}
		}	}

		public virtual void Remove ()
		{
			List<Block> blocksToRemove = new List<Block> ();
			foreach ( KeyValuePair<Vector3, Block> block in m_Blocks )
			{
				if ( block.Value.transform.position.x - m_CurrentX > m_DestroyRange )
				{
					blocksToRemove.Add ( block.Value );
				}
			}
			List<BackgroundBlock> backgroundBlocksToRemove = new List<BackgroundBlock> ();
			foreach ( KeyValuePair<Vector3, BackgroundBlock> block in m_BackgroundBlocks )
			{
				if ( block.Value.transform.position.x - m_FathestBackgroundX > m_DestroyRange )
				{
					backgroundBlocksToRemove.Add ( block.Value );
				}
			}
			for ( int i = 0; i < blocksToRemove.Count; i++ )
			{
				RemoveBlock ( blocksToRemove [ i ] );
			}
			for ( int i = 0; i < backgroundBlocksToRemove.Count; i++ )
			{
				RemoveBackgroundBlock ( backgroundBlocksToRemove [ i ] );
			}
		}

		public virtual void RemoveAll ()
		{
			List<Block> blocksToRemove = new List<Block> ();
			foreach ( KeyValuePair<Vector3, Block> block in m_Blocks )
			{
				blocksToRemove.Add ( block.Value );
			}
			List<BackgroundBlock> backgroundBlocksToRemove = new List<BackgroundBlock> ();
			foreach ( KeyValuePair<Vector3, BackgroundBlock> block in m_BackgroundBlocks )
			{
				backgroundBlocksToRemove.Add ( block.Value );
			}
			for ( int i = 0; i < blocksToRemove.Count; i++ )
			{
				RemoveBlock ( blocksToRemove [ i ] );
			}
			for ( int i = 0; i < backgroundBlocksToRemove.Count; i++ )
			{
				RemoveBackgroundBlock ( backgroundBlocksToRemove [ i ] );
			}
		}

		public virtual void RemoveBlockAt ( Vector3 position )
		{
			RemoveBlock ( m_Blocks [ position ] );
		}

		public virtual void RemoveBlock ( Block block )
		{
			block.OnRemove ( this );
			Destroy ( m_Blocks [ block.transform.position ].gameObject );
			m_Blocks.Remove ( block.transform.position );
		}

		public virtual void RemoveBackgroundBlock ( BackgroundBlock block )
		{
			block.OnRemove ( this );
			Destroy ( m_BackgroundBlocks [ block.transform.position ].gameObject );
			m_BackgroundBlocks.Remove ( block.transform.position );
		}

		public virtual bool CreateBlock ( Block blockPrefab, Vector3 position )
		{
			if ( blockPrefab == null )
			{
				return false;
			}
			blockPrefab.PreGenerate ( this );
			Block block = Instantiate<Block> ( blockPrefab, position, Quaternion.identity );
			m_PreviousX = m_CurrentX;
			m_CurrentX += block.Width;
			m_Blocks.Add ( position, block );
			blockPrefab.PostGenerate ( this );
			m_LastBlock = block;
			return true;
		}

		public virtual bool CreateBackgroundBlock ( BackgroundBlock blockPrefab, Vector3 position, BackgroundLayer layer, int layerIndex )
		{
			if ( blockPrefab == null )
			{
				return false;
			}
			blockPrefab.PreGenerate ( this );
			position.z = blockPrefab.transform.position.z;
			position.y = blockPrefab.transform.position.y;
			BackgroundBlock block = Instantiate<BackgroundBlock> ( blockPrefab, position, Quaternion.identity );
			float width = Random.Range ( block.MinWidth, block.MaxWidth );
			m_BackgroundLayers [ layerIndex ].PreviousX = m_BackgroundLayers [ layerIndex ].CurrentX;
			m_BackgroundLayers [ layerIndex ].CurrentX += width;
			block.Width = width;
			m_BackgroundLayers [ layerIndex ].LastBlock = block;
			m_BackgroundBlocks.Add ( position, block );
			blockPrefab.PostGenerate ( this );
			if ( m_BackgroundLayers [ layerIndex ].CurrentX > m_FathestBackgroundX )
			{
				m_FathestBackgroundX = m_BackgroundLayers [ layerIndex ].CurrentX;
			}
			return true;
		}

		public Block GetCharacterBlock ()
		{
			Block characterBlock = null;
			foreach ( KeyValuePair<Vector3, Block> block in m_Blocks )
			{
				if ( block.Key.x <= m_Character.transform.position.x && block.Key.x + block.Value.Width > m_Character.transform.position.x )
				{
					characterBlock = block.Value;
					break;
				}
			}
			return characterBlock;
		}

		public static Block ChooseFrom ( Block[] blocks )
		{
			if ( blocks == null || blocks.Length <= 0 )
			{
				return null;
			}
			float total = 0;
			for ( int i = 0; i < blocks.Length; i++ )
			{
				if ( blocks [ i ] == null ) continue;
				total += blocks [ i ].Probability;
			}
			if ( total <= 0f ) return null;
			float randomPoint = Random.value * total;
			for ( int i = 0; i < blocks.Length; i++ )
			{
				if ( blocks [ i ] == null ) continue;
				if ( randomPoint < blocks [ i ].Probability )
				{
					return blocks [ i ];
				}
				else
				{
					randomPoint -= blocks [ i ].Probability;
				}
			}
			return blocks [ blocks.Length - 1 ];
		}

		public virtual void ResetPathFollowers ()
		{
			StartCoroutine ( ResetPathFollowersCoroutine () );
		}

		protected IEnumerator ResetPathFollowersCoroutine ()
		{
			int count = 0;
			foreach ( KeyValuePair<Vector3, Block> block in m_Blocks )
			{
				block.Value.Reset ();
				count++;
				// Spread heavy work across frames — yield every 5 blocks
				if ( count % 5 == 0 )
					yield return null;
			}
		}

	}

}