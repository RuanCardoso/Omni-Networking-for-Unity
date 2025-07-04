/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

#if UNITY_EDITOR
using Omni.Core;
using Omni.Editor;
using Omni.Inspector;
using UnityEngine;

[assembly: RegisterTriAttributeDrawer(typeof(NetworkVariableDrawer), TriDrawerOrder.Drawer)]

namespace Omni.Editor
{
    public class HideNetworkVariableDrawerElement : TriElement
    {
        public HideNetworkVariableDrawerElement() { }

        public override float GetHeight(float width)
        {
            return 0f; // hide
        }

        public override void OnGUI(Rect position)
        {
            // hide
        }
    }

    public class NetworkVariableDrawer : TriAttributeDrawer<NetworkVariableAttribute>
    {
        private Texture2D quadTexture;
        public override void OnGUI(Rect position, TriProperty property, TriElement next)
        {
            var propertyContent = property.DisplayNameContent;
            string _propertyContent_text = propertyContent.text;

            if (IsField(property) && !Attribute.HideMode.HasFlag(HideMode.BackingField))
            {
                propertyContent.text = $" {_propertyContent_text}";
                propertyContent.image = GetTexture(Color.red);
            }

            base.OnGUI(position, property, next);
            propertyContent.text = _propertyContent_text;
            propertyContent.image = null;
        }

        public override TriElement CreateElement(TriProperty property, TriElement next)
        {
            var isField = IsField(property);
            var isProperty = !isField;

            HideMode mode = Attribute.HideMode;
            if (mode == HideMode.None)
                return base.CreateElement(property, next);

            if (mode == HideMode.Both)
                return new HideNetworkVariableDrawerElement();

            if (isField && mode.HasFlag(HideMode.BackingField))
                return new HideNetworkVariableDrawerElement();

            if (isProperty && mode.HasFlag(HideMode.Property))
                return new HideNetworkVariableDrawerElement();

            return base.CreateElement(property, next);
        }

        private bool IsField(TriProperty property)
        {
            return property.TryGetAttribute<SerializeField>(out var _) || property.RawName.StartsWith("m_");
        }

        private Texture2D GetTexture(Color color)
        {
            if (quadTexture == null)
            {
                Texture2D whiteTxt = Texture2D.whiteTexture;
                quadTexture = new(whiteTxt.width, whiteTxt.height);

                #region Set Color

                for (int y = 0; y < quadTexture.height; y++)
                {
                    for (int x = 0; x < quadTexture.width; x++)
                    {
                        quadTexture.SetPixel(x, y, color);
                    }
                }

                #endregion

                quadTexture.Apply();
            }

            return quadTexture;
        }
    }
}
#endif