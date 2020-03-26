using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public partial class GraphMaker
{
	public class Editor : UnityEditor.Editor
	{
		public delegate bool DrawInspectorDelegate();
		//static bool blocksOnGenerate = false;
		//static bool diagsOnGenerate = false;
		static GraphMaker graphMaker;
		static Vector2Int dimensions = new Vector2Int();

		public static void OnEnable(Object target)
		{
			graphMaker = target as GraphMaker;

			dimensions = graphMaker.dimensions;
			if (graphMaker.graphMode == GRAPH_MODE.TILE)
				AdjustDimensions(new Vector2Int(-1, -1));
		}

		public static void OnInspectorGUI(DrawInspectorDelegate DrawDefaultInspector)
		{
			GUILayout.Space(3);
			EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
			GRAPH_MODE gmBefore = graphMaker.graphMode;
			graphMaker.graphMode = (GRAPH_MODE)EditorGUILayout.EnumPopup("Graph Type", graphMaker.graphMode);

			if(gmBefore != graphMaker.graphMode)
			{
				dimensions = graphMaker.dimensions;
				if (gmBefore == GRAPH_MODE.POINT && graphMaker.graphMode == GRAPH_MODE.TILE)
					AdjustDimensions(new Vector2Int(-1, -1));
				else if (gmBefore == GRAPH_MODE.TILE && graphMaker.graphMode == GRAPH_MODE.POINT)
					AdjustDimensions(new Vector2Int(1, 1));
			}

			
			dimensions = EditorGUILayout.Vector2IntField("Board Dimensions", dimensions);
			graphMaker.m_tileSize = EditorGUILayout.FloatField("Tile Size", graphMaker.m_tileSize);
			graphMaker.planeMode = (PLANE_MODE)EditorGUILayout.EnumPopup("Graph Plane", graphMaker.planeMode);

			if (graphMaker.graphMode == GRAPH_MODE.POINT)
			{
				graphMaker.blockMode = (BLOCK_MODE)EditorGUILayout.EnumPopup("Block Generation", graphMaker.blockMode);
				graphMaker.diagMode = (DIAG_MODE)EditorGUILayout.EnumPopup("Diagnol Connections", graphMaker.diagMode);
			}

			GUILayout.Space(10);
			EditorGUILayout.LabelField("Generation Control", EditorStyles.boldLabel);
			if (GUILayout.Button("Generate Graph"))
			{
				graphMaker.GenerateBoard(dimensions, graphMaker.tileSize);
				EditorUtility.SetDirty(graphMaker);
			}
			if (graphMaker.graphMode == GRAPH_MODE.POINT)
			{
				if (GUILayout.Button("Create Random Blocked Points"))
					graphMaker.GenerateRandomBlocks();
				if (graphMaker.diagMode != DIAG_MODE.NONE && GUILayout.Button("Create " + graphMaker.diagMode.ToString() + " Diagnol Connections"))
					switch (graphMaker.diagMode)
					{
						case DIAG_MODE.ALL:
							graphMaker.GenerateAllDiagConnections(); break;
						case DIAG_MODE.LEFT:
							graphMaker.GenerateLeftDiagConnections(); break;
						case DIAG_MODE.RIGHT:
							graphMaker.GenerateRightDiagConnections(); break;
						case DIAG_MODE.RANDOM_ALL:
							graphMaker.GenerateRandomDiagConnections(); break;
						case DIAG_MODE.RANDOM_LEFT:
							graphMaker.GenerateRandomLeftDiagConnections(); break;
						case DIAG_MODE.RANDOM_RIGHT:
							graphMaker.GenerateRandomRightDiagConnections(); break;
					}
			}

			DrawDefaultInspector();
		}

		static void AdjustDimensions(Vector2Int amount)
		{
			int max = graphMaker.graphMode == GRAPH_MODE.TILE ? 0 : 1;
			dimensions.x = Mathf.Clamp(dimensions.x + amount.x, max, int.MaxValue);
			dimensions.y = Mathf.Clamp(dimensions.y + amount.y, max, int.MaxValue);
		}
	}
}
