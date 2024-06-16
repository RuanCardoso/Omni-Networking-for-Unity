using System;
using Omni.Core.Interfaces;
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
                        "This transporter is not initialized! Call Initialize() first."
                    );
                }

                return _ITransporter;
            }
            set => _ITransporter = value;
        }
    }
}
