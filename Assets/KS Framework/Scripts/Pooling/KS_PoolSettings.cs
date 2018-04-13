﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KS_Core.Pooling
{
    /// <summary>
    /// Settings container for pool objects
    /// </summary>
    public class PoolObjectSettings
    {
        /// <summary>
        /// The objects identifying tag in the pool
        /// </summary>
        public string tag;
        /// <summary>
        /// Max items of this type can be stored in the pool
        /// </summary>
        public int poolLimit;

        /// <summary>
        /// Initialize new 
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="maxPooledOfType"></param>
        public PoolObjectSettings(string tag, int maxPooledOfType)
        {
            this.tag = tag;
            this.poolLimit = maxPooledOfType;
        }
    }
}
