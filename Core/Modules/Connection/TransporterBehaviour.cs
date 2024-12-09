using Omni.Core.Interfaces;
using System;
using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
    public class TransporterBehaviour : MonoBehaviour
    {
        private ITransporter _ITransporter;

        internal ITransporter ITransporter
        {
            get
            {
                if (_ITransporter == null)
                {
                    throw new NullReferenceException(
                        "The transporter is not initialized. Ensure Initialize() is called before performing any operations."
                    );
                }

                return _ITransporter;
            }
            set => _ITransporter = value;
        }
    }
}