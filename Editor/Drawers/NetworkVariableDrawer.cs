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
using UnityEditor;
using UnityEngine;

namespace Omni.Editor
{
	[CustomPropertyDrawer(typeof(NetworkVariableAttribute), true)]
	[CanEditMultipleObjects]
	public class NetworkVariableDrawer : PropertyDrawer
	{
		private Texture2D quadTexture;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label.text = $" {property.displayName}";
			label.image = GetTexture(Color.red);
			EditorGUI.PropertyField(position, property, label, true);
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
