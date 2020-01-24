using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public partial class GraphMaker : MonoBehaviour
{
	[System.Serializable]
	public class GraphPoint
	{
		[System.Serializable]
		public class ConnectionData
		{
			public int index;
			public float dist;

			public ConnectionData(int index, float distance)
			{
				this.index = index;
				dist = distance;
			}
		}
		[System.Serializable]
		public class NavData
		{
			public int pIndex;
			public bool wasTarget;
			public bool evaluated;
			public float tDist;

			public NavData(int index, float distance)
			{
				pIndex = index;
				tDist = distance;
				wasTarget = false;
				evaluated = false;
			}
		}

		public bool isBlocked = false;
		public Vector3 position = Vector3.zero;
		public NavData navData = null;
		public List<ConnectionData> connections = new List<ConnectionData>();

		public bool IsConnectedTo(int index)
		{
			foreach (var link in connections)
				if (link.index == index)
					return true;
			return false;
		}
	}
	[System.Serializable]
	public class GraphTile
	{
		public int index;
		public Vector3 position = Vector3.zero;
		public List<GraphPoint> cornerPoints = new List<GraphPoint>(4);
	}

	public enum GRAPH_MODE { POINT, TILE }
	public enum DIAG_MODE { NONE, ALL, LEFT, RIGHT, RANDOM_LEFT, RANDOM_RIGHT, RANDOM_ALL }
	public enum BLOCK_MODE { NONE, RANDOM }
	public enum PLANE_MODE { XY, XZ, YZ }

	public bool scanGraph = false;
	[Header("Generation Settings")]
	 Vector2Int m_dimensions;
	 float m_tileSize = 5.0f;
	 GRAPH_MODE graphMode = GRAPH_MODE.POINT;
	 DIAG_MODE diagMode = DIAG_MODE.NONE;
	 BLOCK_MODE blockMode = BLOCK_MODE.NONE;
	 PLANE_MODE planeMode = PLANE_MODE.XY;
	public Vector2Int dimensions { get { return graphMode == GRAPH_MODE.POINT ? m_dimensions : m_dimensions - Vector2Int.one; } }
	public float tileSize { get { return m_tileSize; } }
	[Header("Generation Control")]
	 float diagChance = 0.2f;
	 float blockCreationChance = 0.2f;
	[Header("Graph Information")]
	public int numBlocks;
	public int numConnections;
	public int numPoints { get { return graphPoints.Count; } }
	public List<GraphPoint> graphPoints = new List<GraphPoint>();
	public List<GraphTile> graphTiles = new List<GraphTile>();
	[Header("Graph Debug NavData")]
	 int startIndex = 0;
	 int finalIndex = 0;
	 List<int> navPath = new List<int>();

	private void Awake()
	{
	}

	// Graph Internal Utility
	#region

	void AddPoint(Vector3 pos)
	{
		GraphPoint newPoint = new GraphPoint();
		newPoint.position = pos;
		graphPoints.Add(newPoint);
	}
	void AddTile(Vector3 pos)
	{
		GraphTile tile = new GraphTile();
		tile.position = pos;
		graphTiles.Add(tile);
	}

	void ConnectPoints(int indexA, int indexB)
	{
		float dist = Vector3.Distance(graphPoints[indexA].position, graphPoints[indexB].position);

		foreach (var link in graphPoints[indexA].connections)
			if (link.index == indexB)
				return;

		numConnections++;
		graphPoints[indexA].connections.Add(new GraphPoint.ConnectionData(indexB, dist));
		graphPoints[indexB].connections.Add(new GraphPoint.ConnectionData(indexA, dist));
	}

	void DisconnectPoints(int indexA, int indexB)
	{
		bool success = false;
		foreach (var link in graphPoints[indexA].connections)
			if (link.index == indexB)
			{
				success = true;
				graphPoints[indexA].connections.Remove(link);
				break;
			}
		foreach (var link in graphPoints[indexB].connections)
			if (link.index == indexA)
			{
				success = true;
				graphPoints[indexB].connections.Remove(link);
				break;
			}
		if (success) numConnections--;
	}

	void ClearPointNavData()
	{
		foreach (var point in graphPoints)
			point.navData = new GraphPoint.NavData(0, float.MaxValue);
		navPath.Clear();
	}

	public Vector2 PointPos(int index)
	{
		return graphPoints[index].position;
	}

	#endregion

	#region Generation Functions


	public void GenerateBoard(int width, int height, float squareSize)
	{
		m_tileSize = squareSize;
		switch (graphMode)
		{
			case GRAPH_MODE.POINT:
				m_dimensions.x = width;
				m_dimensions.y = height;
				break;
			case GRAPH_MODE.TILE:
				m_dimensions.x = width + 1;
				m_dimensions.y = height + 1;
				break;
		}
		GenerateBoard();
	}
	public void GenerateBoard(Vector2Int dim, float squareSize)
	{
		m_dimensions = dim;
		m_tileSize = squareSize;
		switch (graphMode)
		{
			case GRAPH_MODE.TILE:
				m_dimensions += Vector2Int.one;
				break;
		}
		GenerateBoard();
	}
	public void GenerateBoard(Vector2Int dim, float squareSize, GRAPH_MODE graphMode, PLANE_MODE planeMode)
	{
		this.graphMode = graphMode;
		this.planeMode = planeMode;
		GenerateBoard(dim, squareSize);
	}
	void GenerateBoard()
	{
		GeneratePoints();
		FinalizeConections();
		ClearPointNavData();
		if (graphMode == GRAPH_MODE.POINT && blockMode == BLOCK_MODE.RANDOM)
			GenerateRandomBlocks();
	}

	Vector3 GetAxisVec()
	{
		Vector3 retval = new Vector3();

		retval.x = planeMode != PLANE_MODE.YZ ? 1 : 0;
		retval.y = planeMode != PLANE_MODE.XZ ? 1 : 0;
		retval.z = planeMode != PLANE_MODE.XY ? 1 : 0;

		return retval;
	}
	Vector3Int GetAxisDimensions()
	{
		Vector3Int retval = new Vector3Int();

		switch (planeMode)
		{
			case PLANE_MODE.XY:
				retval.x = m_dimensions.x;
				retval.y = m_dimensions.y;
				break;
			case PLANE_MODE.XZ:
				retval.x = m_dimensions.x;
				retval.z = m_dimensions.y;
				break;
			case PLANE_MODE.YZ:
				retval.y = m_dimensions.x;
				retval.z = m_dimensions.y;
				break;
		}

		return retval;
	}
	Vector3 GetAxisFromVec2(Vector2 vec2)
	{
		Vector3 retval = new Vector3();

		switch (planeMode)
		{
			case PLANE_MODE.XY:
				retval.x = vec2.x;
				retval.y = vec2.y;
				break;
			case PLANE_MODE.XZ:
				retval.x = vec2.x;
				retval.z = vec2.y;
				break;
			case PLANE_MODE.YZ:
				retval.y = vec2.x;
				retval.z = vec2.y;
				break;
		}

		return retval;
	}

	// index = col + (boardDimensions.y * row)
	void GeneratePoints()
	{
		Vector3 gameBoardScale = (Vector3)GetAxisDimensions() * tileSize;
		Vector3 startPos = transform.position - (gameBoardScale / 2.0f) + (GetAxisVec() * tileSize / 2.0f); // The lower right corner of the board.

		Debug.Log("Scale: " + (Vector2)m_dimensions * tileSize + "\nStartPos: " + startPos);

		if (graphPoints == null)
			graphPoints = new List<GraphPoint>();
		if (graphTiles == null)
			graphTiles = new List<GraphTile>();
		graphPoints.Clear();
		graphTiles.Clear();

		for (int x = 0; x < m_dimensions.x; ++x)
		{
			for (float y = 0; y < m_dimensions.y; ++y)
			{
				AddPoint(startPos + (GetAxisFromVec2(new Vector2(x, y)) * tileSize));
				if (graphMode == GRAPH_MODE.TILE && x < m_dimensions.x - 1 && y < m_dimensions.y-1)
				{
					float halfTileSize = tileSize / 2.0f;
					AddTile(graphPoints[graphPoints.Count-1].position + GetAxisFromVec2(new Vector2(halfTileSize, halfTileSize)));
					graphTiles[graphTiles.Count - 1].index = graphTiles.Count - 1;
				}
			}
		}

		numConnections = numBlocks = 0;
	}

	void FinalizeConections()
	{
		// index = row + (col * boardDimensions.y)
		float mDist = tileSize * tileSize;
		for (int col = 0; col < m_dimensions.x; col++)
		{
			for (int row = 0; row < m_dimensions.y; row++)
			{
				int currIndex = row + (col * m_dimensions.y);
				if (row < m_dimensions.y - 1)
					ConnectPoints(currIndex, currIndex + 1);
				if (col < m_dimensions.x - 1)
					ConnectPoints(currIndex, row + ((col + 1) * m_dimensions.y));
			}
		}
		switch (diagMode)
		{
			case DIAG_MODE.ALL:
				GenerateAllDiagConnections(); break;
			case DIAG_MODE.LEFT:
				GenerateLeftDiagConnections(); break;
			case DIAG_MODE.RIGHT:
				GenerateRightDiagConnections();break;
			case DIAG_MODE.RANDOM_ALL:
				GenerateRandomDiagConnections(); break;
			case DIAG_MODE.RANDOM_LEFT:
				GenerateRandomLeftDiagConnections(); break;
			case DIAG_MODE.RANDOM_RIGHT:
				GenerateRandomRightDiagConnections(); break;
		}
	}

	void GenerateAllDiagConnections()
	{
		for (int col = 0; col < m_dimensions.x; col++)
		{
			for (int row = 0; row < m_dimensions.y; row++)
			{
				int currIndex = row + (col * m_dimensions.y);
				if (row < m_dimensions.y - 1)
				{
					if (col != 0)
						ConnectPoints(currIndex, (col - 1) + ((row + 1) * m_dimensions.y));
					if (col < m_dimensions.x - 1)
						ConnectPoints(currIndex, (col + 1) + ((row + 1) * m_dimensions.y));
				}
			}
		}
	}
	void GenerateLeftDiagConnections()
	{
		for (int col = 0; col < m_dimensions.x; col++)
		{
			for (int row = 0; row < m_dimensions.y; row++)
			{
				int currIndex = row + (col * m_dimensions.y);
				if (row < m_dimensions.y - 1 && col != 0)
					ConnectPoints(currIndex, (col - 1) + ((row + 1) * m_dimensions.y));
			}
		}
	}
	void GenerateRightDiagConnections()
	{
		for (int col = 0; col < m_dimensions.x; col++)
		{
			for (int row = 0; row < m_dimensions.y; row++)
			{
				int currIndex = row + (col * m_dimensions.y);
				if (row < m_dimensions.y - 1 && col < m_dimensions.x - 1)
					ConnectPoints(currIndex, (col + 1) + ((row + 1) * m_dimensions.y));
			}
		}
	}
	void GenerateRandomDiagConnections()
	{
		for (int col = 0; col < m_dimensions.x; col++)
		{
			for (int row = 0; row < m_dimensions.y; row++)
			{
				int currIndex = row + (col * m_dimensions.y);
				if (row < m_dimensions.y - 1)
				{
					if (col != 0 && Random.value < diagChance)
						ConnectPoints(currIndex, (col - 1) + ((row + 1) * m_dimensions.y));
					if (col < m_dimensions.x - 1 && Random.value < diagChance)
						ConnectPoints(currIndex, (col + 1) + ((row + 1) * m_dimensions.y));
				}
			}
		}
	}
	void GenerateRandomLeftDiagConnections()
	{
		for (int col = 0; col < m_dimensions.x; col++)
		{
			for (int row = 0; row < m_dimensions.y; row++)
			{
				int currIndex = row + (col * m_dimensions.y);
				if (row < m_dimensions.y - 1)
				{
					if (col != 0 && Random.value < diagChance)
						ConnectPoints(currIndex, (col - 1) + ((row + 1) * m_dimensions.y));
				}
			}
		}
	}
	void GenerateRandomRightDiagConnections()
	{
		for (int col = 0; col < m_dimensions.x; col++)
		{
			for (int row = 0; row < m_dimensions.y; row++)
			{
				int currIndex = row + (col * m_dimensions.y);
				if (row < m_dimensions.y - 1)
				{
					if (col < m_dimensions.x - 1 && Random.value < diagChance)
						ConnectPoints(currIndex, (col + 1) + ((row + 1) * m_dimensions.y));
				}
			}
		}
	}

	void GenerateRandomBlocks()
	{
		foreach (var point in graphPoints)
			if (point.connections.Count > 2)
			{
				bool valid = true;

				foreach (var link in point.connections)
					if (graphPoints[link.index].connections.Count <= 2)
						valid = false;

				if (valid && graphPoints.IndexOf(point) != 0 && graphPoints.IndexOf(point) != 99)
				{
					float chance = Random.value;
					if (Random.value <= blockCreationChance)
					{
						point.isBlocked = true;
						Debug.Log("Connection count: " + point.connections.Count);
						while (point.connections.Count > 0)
							DisconnectPoints(graphPoints.IndexOf(point), point.connections[0].index);
						numBlocks++;
					}
				}
			}
	}
	#endregion

	// Graph Navigation.
	#region

	void NavigateBetweenAstar(int indexA, int indexB)
	{
		int nextIndex = indexB;
		var q = new List<int>();
		int cIndex = 0;
		bool foundTarget = false;
		float dist = float.MaxValue;

		startIndex = indexA;
		finalIndex = indexB;

		if (startIndex == finalIndex)
			return;

		ClearPointNavData();
		graphPoints[indexA].navData = new GraphPoint.NavData(indexA, 0.0f);
		graphPoints[indexB].navData.wasTarget = true;

		foreach (var link in graphPoints[indexA].connections)
		{
			float comp = Mathc.SqrDist2D(PointPos(link.index), PointPos(indexB));
			if (comp < dist)
			{
				dist = comp;
				cIndex = graphPoints[indexA].connections.IndexOf(link);
			}
			else
				graphPoints[link.index].navData.evaluated = true;
		}

		graphPoints[indexA].navData.evaluated = true;
		dist = graphPoints[indexA].connections[cIndex].dist;
		cIndex = graphPoints[indexA].connections[cIndex].index;
		graphPoints[cIndex].navData = new GraphPoint.NavData(indexA, dist);
		q.Add(cIndex);

		while (q.Count > 0)
		{
			int curIndex = q[0];

			if (curIndex == indexB)
				foundTarget = true;
			else if (graphPoints[curIndex].navData.evaluated)
			{
				q.Remove(curIndex);
				continue;
			}

			cIndex = 0;
			dist = float.MaxValue;
			foreach (var link in graphPoints[curIndex].connections)
			{
				if (graphPoints[link.index].navData.evaluated)
					continue;

				float comp = Mathc.SqrDist2D(PointPos(link.index), PointPos(indexB));
				if (comp < dist)
				{
					dist = comp;
					cIndex = graphPoints[curIndex].connections.IndexOf(link);
				}
				else
					graphPoints[link.index].navData.evaluated = true;
			}

			dist = graphPoints[curIndex].connections[cIndex].dist + graphPoints[curIndex].navData.tDist;
			cIndex = graphPoints[curIndex].connections[cIndex].index;

			if (dist < graphPoints[cIndex].navData.tDist)
				graphPoints[cIndex].navData = new GraphPoint.NavData(curIndex, dist);

			if (!foundTarget)
				q.Add(cIndex);

			graphPoints[curIndex].navData.evaluated = true;
			q.Remove(curIndex);
		}

		// Record path.
		q.Clear();
		q.Add(nextIndex);
		for (int a = 0; a < graphPoints.Count && nextIndex != indexA; a++)
		{
			nextIndex = graphPoints[nextIndex].navData.pIndex;
			q.Add(nextIndex);
		}


		navPath.Clear();
		for (int a = 0; a < q.Count; a++)
			navPath.Add(q[q.Count - (a + 1)]);
	}

	List<GraphPoint.ConnectionData> links = new List<GraphPoint.ConnectionData>(4);
	void NavigateBetweenDijk(int indexA, int indexB)
	{
		int nextIndex = indexB;
		var q = new List<int>();
		bool foundTarget = false;
		float dist = float.MaxValue;

		startIndex = indexA;
		finalIndex = indexB;

		if (startIndex == finalIndex)
			return;

		ClearPointNavData();
		graphPoints[indexA].navData = new GraphPoint.NavData(indexA, 0.0f);
		graphPoints[indexA].navData.evaluated = true;
		graphPoints[indexB].navData.wasTarget = true;

		links.Clear();
		foreach (var link in graphPoints[indexA].connections)
			links.Add(link);
		while (links.Count > 0)
		{
			int rIndex = Random.Range(0, links.Count);
			var link = links[rIndex];
			graphPoints[link.index].navData = new GraphPoint.NavData(indexA, link.dist);
			q.Add(link.index);
			links.RemoveAt(rIndex);
		}

		while (q.Count > 0)
		{
			int curIndex = q[0];

			if (curIndex == indexB)
				foundTarget = true;
			else if (graphPoints[curIndex].navData.evaluated)
			{
				q.Remove(curIndex);
				continue;
			}

			dist = float.MaxValue;
			links.Clear();
			foreach (var link in graphPoints[curIndex].connections)
				links.Add(link);
			while (links.Count > 0)
			{
				int rIndex = Random.Range(0, links.Count);
				var link = links[rIndex];

				dist = link.dist + graphPoints[curIndex].navData.tDist;
				if (dist < graphPoints[link.index].navData.tDist)
					graphPoints[link.index].navData = new GraphPoint.NavData(curIndex, dist);
				if (!foundTarget && !graphPoints[link.index].navData.evaluated)
					q.Add(link.index);

				links.RemoveAt(rIndex);
			}

			graphPoints[curIndex].navData.evaluated = true;
			q.Remove(curIndex);
		}

		// Record path.
		q.Clear();
		q.Add(nextIndex);
		for (int a = 0; a < graphPoints.Count && nextIndex != indexA; a++)
		{
			nextIndex = graphPoints[nextIndex].navData.pIndex;
			q.Add(nextIndex);
		}


		navPath.Clear();
		for (int a = 0; a < q.Count; a++)
			navPath.Add(q[q.Count - (a + 1)]);
	}

	public List<int> GetPath(Vector2 startPos, Vector2 endPos)
	{
		List<int> retval = new List<int>();

		NavigateBetweenDijk(GetClosestPointTo(startPos), GetClosestPointTo(endPos));

		for (int a = 0; a < navPath.Count; a++)
			retval.Add(navPath[a]);

		return retval;

	}

	/// <summary> Generate a random path with the closest point to 'pos' being the start.  </summary>
	public List<int> GetRandomPathFrom(Vector2 pos)
	{
		int indexA = GetClosestPointTo(pos);
		int indexB = Random.Range(0, graphPoints.Count - 1);
		List<int> retval = new List<int>();

		while (graphPoints[indexB].isBlocked)
			indexB = Random.Range(0, graphPoints.Count - 1); ;

		NavigateBetweenAstar(indexA, indexB);

		for (int a = 0; a < navPath.Count; a++)
			retval.Add(navPath[a]);

		return retval;
	}

	#endregion

	// Misc Public Graph Data
	#region

	public int GetClosestPointTo(Vector2 pos)
	{
		int cIndex = 0;
		float dist = float.MaxValue;
		foreach (var point in graphPoints)
		{
			if (point.isBlocked)
				continue;
			float comp = Mathc.SqrDist2D(point.position, pos);
			if (comp < dist)
			{
				dist = comp;
				cIndex = graphPoints.IndexOf(point);
			}
		}

		return cIndex;
	}

	/// <summary>  Returns the closest point to "pos" that is connected to "index"   </summary>
	int GetClosestConnectedPoint(int index, Vector2 pos)
	{
		int cIndex = 0;
		float dist = 0;
		foreach (var link in graphPoints[index].connections)
		{
			if (graphPoints[link.index].isBlocked)
				continue;
			float comp = Mathc.SqrDist2D(PointPos(link.index), pos);
			if (comp < dist)
			{
				dist = comp;
				cIndex = link.index;
			}
		}

		return cIndex;
	}

	public bool ScanGraphForBlocks()
	{
		bool foundBlock = false;
		foreach (var point in graphPoints)
			if (point.isBlocked)
			{
				while (point.connections.Count > 0)
					DisconnectPoints(graphPoints.IndexOf(point), point.connections[0].index);
				foundBlock = true;
			}
		ClearPointNavData();
		return foundBlock;
	}

	#endregion

	private void OnDrawGizmos()
	{
		if (graphPoints != null)
		{
			foreach (var point in graphPoints)
			{
				if (point.isBlocked)
					Gizmos.color = Color.black;
				else if (point.navData.evaluated)
					Gizmos.color = Color.cyan;
				else
					Gizmos.color = Color.red;
				Gizmos.DrawSphere(point.position, 1.0f);
			}
			if (graphMode == GRAPH_MODE.TILE)
			{
				Gizmos.color = Color.white;
				foreach (var tile in graphTiles)
					Gizmos.DrawSphere(tile.position, Mathf.Max(1.0f, tileSize / 10.0f));
			}	

			foreach (var point in graphPoints)
				if (point.connections != null)
					foreach (var link in point.connections)
					{
						if (link.index < graphPoints.IndexOf(point))
						{
							if (graphPoints[link.index].navData.evaluated)
								Gizmos.color = Color.cyan;
							else
								Gizmos.color = Color.magenta;
							Gizmos.DrawLine(graphPoints[link.index].position, point.position);
						}
					}

			if (navPath.Count > 0)
			{
				Gizmos.color = Color.green;
				for (int a = 0; a < navPath.Count - 1; a++)
					Gizmos.DrawLine(PointPos(navPath[a]), PointPos(navPath[a + 1]));
				foreach (var index in navPath)
				{
					Gizmos.color = Color.green;
					if (index == startIndex)
						Gizmos.color = Color.blue;
					else if (index == finalIndex)
						Gizmos.color = Color.yellow;

					Gizmos.DrawSphere(PointPos(index), 1.0f);
				}
			}
		}
	}

	// y + (x * boardDimensions.y)
	public GraphPoint GetGraphPoint(int x, int y)
	{
		return graphPoints[y + (x * m_dimensions.y)];
	}

	public bool IsPosInGridPos(Vector2 pos, int gridX, int gridY)
	{
		return pos.x >= gridX * tileSize && pos.x <= gridX * (tileSize + 1)
			&& pos.y >= gridY * tileSize && pos.y <= gridY * (tileSize + 1);
	}

	public GraphTile GetGraphTile(int x, int y)
	{
		return graphTiles[y + (x * m_dimensions.y)];
	}
}
