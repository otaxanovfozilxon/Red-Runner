using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedRunner.UI
{
    public class LoadingScreen : UIScreen
    {
        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);

            if (open)
            {
                // Randomize the color each time the loading screen opens
                var imageRandom = GetComponentInChildren<UIImageRandom>();
                if (imageRandom != null)
                {
                    imageRandom.Randomize();
                }
            }
        }
    }

}
