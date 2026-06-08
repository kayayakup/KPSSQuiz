using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MillionaireGame
{
    [AddComponentMenu("UI/Effects/Gradient")]
    public class UIGradient : BaseMeshEffect
    {
        [SerializeField] private Color _colorTop = Color.white;
        [SerializeField] private Color _colorBottom = Color.black;

        public Color colorTop
        {
            get => _colorTop;
            set
            {
                _colorTop = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public Color colorBottom
        {
            get => _colorBottom;
            set
            {
                _colorBottom = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            var count = vh.currentVertCount;
            if (count == 0) return;

            var vertexList = new List<UIVertex>();
            for (int i = 0; i < count; i++)
            {
                var vertex = new UIVertex();
                vh.PopulateUIVertex(ref vertex, i);
                vertexList.Add(vertex);
            }

            float bottomY = vertexList[0].position.y;
            float topY = vertexList[0].position.y;

            for (int i = 1; i < count; i++)
            {
                float y = vertexList[i].position.y;
                if (y > topY) topY = y;
                if (y < bottomY) bottomY = y;
            }

            float uiElementHeight = topY - bottomY;
            if (uiElementHeight <= 0f) return; // Avoid division by zero

            for (int i = 0; i < count; i++)
            {
                var uiVertex = vertexList[i];
                float t = (uiVertex.position.y - bottomY) / uiElementHeight;
                Color gradColor = Color.Lerp(_colorBottom, _colorTop, t);
                // Preserve original vertex alpha
                gradColor.a = gradColor.a * (uiVertex.color.a / 255f);
                uiVertex.color = gradColor;
                vh.SetUIVertex(uiVertex, i);
            }
        }
    }
}
