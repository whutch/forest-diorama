using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class Generator : MonoBehaviour {

	public LayerMask zoneLayer;
	public Camera mainCamera;
	public GameObject cameraFocus;
	public float cameraAngle = 180.0f;
    public float cameraTilt = 65.0f;
    public float cameraDistance = 50.0f;
    public float rotationSpeed = 0.5f;
    public float smoothTime = 0.5f;
	public int seed = 12345;

	Tuple<string, float>[] tiles;
	List<int> tilesThatNeedFeatures;
	private int riverLength = 0;
	private bool hasPlacedBridge = false;
	private bool cameraGoing = true;
	private Vector3 cameraWasAt;
	private GameObject generatedObjects;

	void Start() {
		GameObject.Find("Options").SetActive(false);
		generatedObjects = GameObject.Find("Generated Objects");
		NewSeed();
		GenerateDiorama();
	}

	void Update() {
		if (Input.GetKeyUp(KeyCode.Q)) {
			Application.Quit();
		}
		if (Input.GetKeyUp(KeyCode.N)) {
			NewSeed();
			GenerateDiorama();
		}
		if (Input.GetKeyUp(KeyCode.R)) {
			cameraAngle = 180.0f;
		}
		if (Input.GetKeyUp(KeyCode.Space)) {
			cameraGoing = !cameraGoing;
		}
		if (cameraGoing) {
			cameraAngle += rotationSpeed;
            cameraAngle = CircularClamp(cameraAngle, 0, 360);
			Vector3 newPosition = CalculateSphericalPoint(cameraAngle, cameraTilt, cameraDistance);
			newPosition += cameraFocus.transform.position;
			mainCamera.transform.position = Vector3.Slerp(mainCamera.transform.position, newPosition, smoothTime);
			mainCamera.transform.LookAt(cameraFocus.transform);
		}
	}

    Vector3 CalculateSphericalPoint(float longitude, float latitude, float radius) {
        longitude *= Mathf.Deg2Rad;
        latitude *= Mathf.Deg2Rad;
        float x = radius * Mathf.Sin(latitude) * Mathf.Cos(longitude);
        float y = radius * Mathf.Sin(latitude) * Mathf.Sin(longitude);
        float z = radius * Mathf.Cos(latitude);
        return new Vector3(x, z, y);
    }


    float CircularClamp(float value, float min, float max) {
        float range = max - min;
        if (value < min)
            return range - value;
        if (value >= max)
            return value - range;
        return value;
    }

	public void NewSeed() {
		seed = Random.Range(10000, 100000);
	}

	public void CameraGo() {
		if (cameraGoing) {
			cameraGoing = false;
			mainCamera.transform.position = cameraWasAt;
		}
		else {
			cameraWasAt = mainCamera.transform.position;
			cameraGoing = true;
		}
	}

	public void GenerateDiorama() {
		// Delete any existing objects.
		foreach (Transform child in generatedObjects.transform) {
			child.gameObject.SetActive(false);
			GameObject.Destroy(child.gameObject);
		}
		tiles = new Tuple<string, float>[9];
		tilesThatNeedFeatures = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
		hasPlacedBridge = false;
		Random.InitState(seed);
		GenerateTerrain();
		GenerateMountains();
		CheckHighlanders();
		CheckRandomFeatures();
		GenerateTrees();
		GenerateClutter();
		GenerateGrass();
	}

	void GenerateTerrain() {
		var possibleRiverTiles = new int[] { 3, 6, 7, 8, 5 };
		var possibleFeatureTiles = new List<string> {
			"Feature Camp",
			"Feature Boulder",
			"Feature Stone Ring",
			"Feature Logging",
			"Feature Pond",
			"Feature Flower Patch",
		};

		// Background tiles are all simple ground.
		for (int i = 0; i < 3; i++) {
			tiles[i] = Tuple.Create("Ground", 0f);
			tilesThatNeedFeatures.Remove(i);
		}

		// Calculate the length of the river.
		Tuple<int, object>[] riverLengthOptions = {
			Tuple.Create(3, (object) 0),
			Tuple.Create(2, (object) 1),
			Tuple.Create(3, (object) 2),
			Tuple.Create(5, (object) 3),
			Tuple.Create(4, (object) 4),
		};
		riverLength = (int) WeightedRandom(riverLengthOptions);
		int riverStart = Random.Range(0, possibleRiverTiles.Length - riverLength + 1);
		bool hasSpring = riverLength == 1 || Random.value <= 0.4;
		bool springAtStart = false;
		if (hasSpring && Random.value <= 0.5) {
			springAtStart = true;
		}
		for (int i = riverStart; i < riverStart + riverLength; i++) {
			int tile = possibleRiverTiles[i];
			bool isStart = tile == possibleRiverTiles[riverStart];
			bool isEnd = tile == possibleRiverTiles[riverStart + riverLength - 1];
			bool isSpring = hasSpring && ((isStart && springAtStart) || (isEnd && !springAtStart));
			tiles[tile] = GenerateRiverTile(tile, isStart, isEnd, isSpring);
			tilesThatNeedFeatures.Remove(tile);
		}

		// Add features to tiles that need them.
		float groundChance = 0f;
		for (int i = 0; i < tilesThatNeedFeatures.Count; i++) {
			string feature = possibleFeatureTiles[Random.Range(0, possibleFeatureTiles.Count)];
			if (Random.value <= groundChance) {
				feature = "Ground";
			}
			else {
				possibleFeatureTiles.Remove(feature);
				groundChance += 0.33f;
			}
			tiles[tilesThatNeedFeatures[i]] = Tuple.Create(feature, 90f * Random.Range(-1, 1));
		}

		// Place all the generated tiles.
		for (int i = 0; i < 9; i++) {
			GameObject obj = (GameObject) Instantiate(Resources.Load($"prefabs/{tiles[i].Item1}"));
			Vector3 pos = new Vector3();
			pos.x = -20f + ((i % 3) * 20);
			pos.z = 20f - (Mathf.Floor(i / 3) * 20);
			obj.transform.position = pos;
			obj.transform.rotation = Quaternion.Euler(0f, tiles[i].Item2, 0f);
			obj.transform.parent = generatedObjects.transform;
		}
	}

	string GenerateBentRiverTile() {
		if (Random.value <= 0.4) {
			return "River Bend Bank";
		}
		return "River Bend";
	}

	string GenerateStraightRiverTile() {
		if (!hasPlacedBridge && Random.value <= 0.4) {
			hasPlacedBridge = true;
			if (Random.value <= 0.3) {
				return "River Bridge Straight";
			}
			return "River Bridge Arch";
		}
		if (Random.value <= 0.4) {
			return "River Banks";
		}
		return "River";
	}

	Tuple<string, float> GenerateRiverTile(int tileIndex, bool isStart, bool isEnd, bool isSpring) {
		// I'm not proud of this.
		if (tileIndex == 3) {
			if (isSpring) {
				return Tuple.Create("River Spring", riverLength == 1 ? 180f : 90f);
			}
			return Tuple.Create(GenerateBentRiverTile(), 0f);
		}
		if (tileIndex == 5) {
			if (isSpring) {
				return Tuple.Create("River Spring", riverLength == 1 ? 0f : 90f);
			}
			return Tuple.Create(GenerateBentRiverTile(), -90f);
		}
		if (tileIndex == 6) {
			if (isEnd) {
				if (isSpring) {
					return Tuple.Create("River Spring", riverLength == 1 ? 180f : -90f);
				}
				return Tuple.Create(GenerateBentRiverTile(), 90f);
			}
			if (isStart) {
				if (isSpring) {
					return Tuple.Create("River Spring", riverLength == 1 ? 90f : 0f);
				}
				if (Random.value <= 0.5) {
					return Tuple.Create(GenerateStraightRiverTile(), 0f);
				}
				return Tuple.Create(GenerateBentRiverTile(), -90f);
			}
			return Tuple.Create(GenerateBentRiverTile(), 180f);
		}
		if (tileIndex == 7) {
			if (isStart) {
				if (isSpring) {
					return Tuple.Create("River Spring", riverLength == 1 ? 90f : 0f);
				}
				return Tuple.Create(GenerateBentRiverTile(), -90f);
			}
			if (isEnd) {
				if (isSpring) {
					return Tuple.Create("River Spring", riverLength == 1 ? 90f : 180f);
				}
				return Tuple.Create(GenerateBentRiverTile(), 0f);
			}
			return Tuple.Create(GenerateStraightRiverTile(), 0f);
		}
		if (tileIndex == 8) {
			if (isStart) {
				if (isSpring) {
					return Tuple.Create("River Spring", riverLength == 1 ? 0f : -90f);
				}
				return Tuple.Create(GenerateBentRiverTile(), 180f);
			}
			if (isEnd) {
				if (isSpring) {
					return Tuple.Create("River Spring", riverLength == 1 ? 90f : 180f);
				}
				if (Random.value <= 0.5) {
					return Tuple.Create(GenerateStraightRiverTile(), 0f);
				}
				return Tuple.Create(GenerateBentRiverTile(), 0f);
			}
			return Tuple.Create(GenerateBentRiverTile(), 90f);
		}
		// Should hopefully not get here.
		Debug.LogError("Failed to generate a river tile.");
		return Tuple.Create("Ground", 0f);
	}

	string[] mountainPrefabs = {
		"Mountain Big 1",
		"Mountain Big 2",
		"Mountain Big 3",
		"Mountain Big 4",
		"Mountain Big 5",
		"Mountain Big 6",
	};

	void GenerateMountains() {
		for (int i = 0; i < 8; i++) {
			string prefabName = mountainPrefabs[Random.Range(0, mountainPrefabs.Length)];
			GameObject mountain = (GameObject) Instantiate(Resources.Load($"prefabs/{prefabName}"), generatedObjects.transform);
			float heightScale = Random.Range(2f, 3f);
			float widthScale = Random.Range(1.5f, 3f);
			mountain.transform.localScale = new Vector3(widthScale, heightScale, 1.5f);
			float angle = Random.Range(-30f, 30f);
			mountain.transform.rotation = Quaternion.Euler(0f, angle, 0f);
			mountain.transform.position = new Vector3(-22f + (i * 6.2f), 2.5f, Random.Range(21.5f, 25f));
		}
	}

	void GenerateTrees() {
		var prefabs = new List<string> { "Pine Tall 1", "Pine Tall 2" };
		var points = GeneratePoisson(60f, 20f, 2.5f, 20);
		var anchor = new Vector3(-30f, 0, 10f);
		foreach (var point in points) {
			var anchoredPoint = new Vector3(point.x, 2.5f, point.z) + anchor;
			if (CollidesWithSolid(anchoredPoint) || CollidesWithZone(anchoredPoint, "No Clutter")) {
				continue;
			}
			string prefabName = prefabs[Random.Range(0, prefabs.Count)];
			GameObject tree = (GameObject) Instantiate(Resources.Load($"prefabs/{prefabName}"), generatedObjects.transform);
			float heightScale = Random.Range(0.9f, 1.1f);
			float widthScale = Random.Range(0.9f, 1.1f);
			tree.transform.localScale = new Vector3(widthScale, heightScale, widthScale);
			float xRotation = Random.Range(-4f, 4f);
			float zRotation = Random.Range(-4f, 4f);
			float yRotation = Random.Range(0f, 360f);
			tree.transform.rotation = Quaternion.Euler(xRotation, yRotation, zRotation);
			anchoredPoint.y += Random.Range(-3.5f, 0f);
			tree.transform.position = anchoredPoint;
		}
		prefabs = new List<string> { "Pine Medium 1", "Pine Medium 2", "Pine Medium 1", "Pine Medium 2", "Pine Small 3" };
		points = GeneratePoisson(60f, 20f, 4.5f, 20);
		anchor = new Vector3(-30f, 0, -10f);
		foreach (var point in points) {
			var anchoredPoint = new Vector3(point.x, 2.5f, point.z) + anchor;
			if (CollidesWithSolid(anchoredPoint) || CollidesWithZone(anchoredPoint, "No Clutter")) {
				continue;
			}
			string prefabName = prefabs[Random.Range(0, prefabs.Count)];
			GameObject tree = (GameObject) Instantiate(Resources.Load($"prefabs/{prefabName}"), generatedObjects.transform);
			float heightScale = Random.Range(0.9f, 1.1f);
			float widthScale = Random.Range(0.9f, 1.1f);
			tree.transform.localScale = new Vector3(widthScale, heightScale, widthScale);
			float xRotation = Random.Range(-4f, 4f);
			float zRotation = Random.Range(-4f, 4f);
			float yRotation = Random.Range(0f, 360f);
			tree.transform.rotation = Quaternion.Euler(xRotation, yRotation, zRotation);
			anchoredPoint.y += Random.Range(-2.5f, 0f);
			tree.transform.position = anchoredPoint;
		}
		points = GeneratePoisson(60f, 20f, 7f, 20);
		anchor = new Vector3(-30f, 0, -30f);
		foreach (var point in points) {
			var anchoredPoint = new Vector3(point.x, 2.5f, point.z) + anchor;
			if (CollidesWithSolid(anchoredPoint) || CollidesWithZone(anchoredPoint, "No Clutter")) {
				continue;
			}
			string prefabName = prefabs[Random.Range(0, prefabs.Count)];
			GameObject tree = (GameObject) Instantiate(Resources.Load($"prefabs/{prefabName}"), generatedObjects.transform);
			float heightScale = Random.Range(0.9f, 1.1f);
			float widthScale = Random.Range(0.9f, 1.1f);
			tree.transform.localScale = new Vector3(widthScale, heightScale, widthScale);
			float xRotation = Random.Range(-4f, 4f);
			float zRotation = Random.Range(-4f, 4f);
			float yRotation = Random.Range(0f, 360f);
			tree.transform.rotation = Quaternion.Euler(xRotation, yRotation, zRotation);
			anchoredPoint.y += Random.Range(-1.5f, 0f);
			tree.transform.position = anchoredPoint;
		}
	}

	void GenerateClutter() {
		Tuple<int, object>[] clutterOptions = {
			Tuple.Create(1, (object) "Stone Large 1"),
			Tuple.Create(1, (object) "Stone Large 2"),
			Tuple.Create(1, (object) "Stone Large 3"),
			Tuple.Create(1, (object) "Stone Large 4"),
			Tuple.Create(1, (object) "Stone Large 5"),
			Tuple.Create(1, (object) "Stone Large 6"),
			Tuple.Create(2, (object) "Stone Flat 1"),
			Tuple.Create(2, (object) "Stone Flat 2"),
			Tuple.Create(2, (object) "Stone Flat 3"),
			Tuple.Create(4, (object) "Stone Small 1"),
			Tuple.Create(4, (object) "Stone Small 2"),
			Tuple.Create(4, (object) "Stone Small 3"),
			Tuple.Create(3, (object) "Stump 1"),
			Tuple.Create(3, (object) "Stump 2"),
			Tuple.Create(3, (object) "Stump 3"),
			Tuple.Create(2, (object) "Mushroom Clump 1"),
			Tuple.Create(2, (object) "Mushroom Clump 2"),
			Tuple.Create(2, (object) "Mushroom Clump 3"),
			Tuple.Create(2, (object) "Mushroom Clump 4"),
			Tuple.Create(3, (object) "Flower Clump 1"),
			Tuple.Create(3, (object) "Flower Clump 2"),
			Tuple.Create(3, (object) "Flower Clump 3"),
			Tuple.Create(3, (object) "Flower Clump 4"),
			Tuple.Create(3, (object) "Flower Clump 5"),
			Tuple.Create(4, (object) "Bush 1"),
			Tuple.Create(4, (object) "Bush 2"),
			Tuple.Create(4, (object) "Bush 3"),
			Tuple.Create(4, (object) "Bush 4"),
			Tuple.Create(2, (object) "Bush 5"),
			Tuple.Create(2, (object) "Bush 6"),
			Tuple.Create(2, (object) "Bush 7"),
			Tuple.Create(2, (object) "Bush 8"),
		};
		var points = GeneratePoisson(60f, 60f, 4f, 20);
		var anchor = new Vector3(-30f, 0, -30f);
		foreach (var point in points) {
			var anchoredPoint = new Vector3(point.x, 2.5f, point.z) + anchor;
			if (CollidesWithSolid(anchoredPoint) || CollidesWithZone(anchoredPoint, "No Clutter")) {
				continue;
			}
			string prefabName = (string) WeightedRandom(clutterOptions);
			GameObject tree = (GameObject) Instantiate(Resources.Load($"prefabs/{prefabName}"), generatedObjects.transform);
			float scale = Random.Range(0.9f, 1.1f);
			tree.transform.localScale = new Vector3(scale, scale, scale);
			float yRotation = Random.Range(0f, 360f);
			tree.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
			tree.transform.position = anchoredPoint;
		}
	}

	void GenerateGrass() {
		var prefabs = new List<string> { "Grass Filler 1", "Grass Filler 2", "Grass Filler 3", "Grass Filler 4" };
		var points = GeneratePoisson(60f, 60f, 0.5f, 20);
		var anchor = new Vector3(-30f, 0, -30f);
		foreach (var point in points) {
			var anchoredPoint = new Vector3(point.x, 2.5f, point.z) + anchor;
			if (CollidesWithSolid(anchoredPoint) || CollidesWithZone(anchoredPoint, "No Grass")) {
				continue;
			}
			string prefabName = prefabs[Random.Range(0, prefabs.Count)];
			GameObject tree = (GameObject) Instantiate(Resources.Load($"prefabs/{prefabName}"), generatedObjects.transform);
			float heightScale = Random.Range(0.9f, 1.1f);
			float widthScale = Random.Range(0.9f, 1.1f);
			tree.transform.localScale = new Vector3(widthScale, heightScale, widthScale);
			float yRotation = Random.Range(0f, 360f);
			tree.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
			tree.transform.position = anchoredPoint;
		}
		prefabs = new List<string> { "Grass Clump 1", "Grass Clump 2", "Grass Clump 3", "Grass Clump 4" };
		points = GeneratePoisson(60f, 60f, 2.5f, 20);
		anchor = new Vector3(-30f, 0, -30f);
		foreach (var point in points) {
			var anchoredPoint = new Vector3(point.x, 2.5f, point.z) + anchor;
			if (CollidesWithSolid(anchoredPoint) || CollidesWithZone(anchoredPoint, "No Grass")) {
				continue;
			}
			string prefabName = prefabs[Random.Range(0, prefabs.Count)];
			GameObject tree = (GameObject) Instantiate(Resources.Load($"prefabs/{prefabName}"), generatedObjects.transform);
			float heightScale = Random.Range(0.9f, 1.1f);
			float widthScale = Random.Range(0.9f, 1.1f);
			tree.transform.localScale = new Vector3(widthScale, heightScale, widthScale);
			float yRotation = Random.Range(0f, 360f);
			tree.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
			tree.transform.position = anchoredPoint;
		}
	}

	void CheckHighlanders() {
		var names = new Dictionary<string, List<GameObject>>();
		foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Highlander"))
		{
			if (!IsGeneratedObject(obj)) {
				continue;
			}
			if (!names.ContainsKey(obj.name)) {
				names.Add(obj.name, new List<GameObject>());
			}
			names[obj.name].Add(obj);
		}
		foreach (KeyValuePair<string, List<GameObject>> group in names) {
			int highlander = Random.Range(0, group.Value.Count);
			group.Value.RemoveAt(highlander);
			for (int i = 0; i < group.Value.Count; i++) {
				group.Value[i].SetActive(false);
				Destroy(group.Value[i]);
			}
		}
	}

	void CheckRandomFeatures() {
		foreach (GameObject set in GameObject.FindGameObjectsWithTag("RandomFeatureSet"))
		{
			if (!IsGeneratedObject(set)) {
				continue;
			}
			for (int i = 0; i < set.transform.childCount; i++) {
				Transform child = set.transform.GetChild(i);
				if (Random.value <= 0.35f) {
					child.gameObject.SetActive(false);
					Destroy(child.gameObject);
				}
			}
		}
	}

	object WeightedRandom(Tuple<int, object>[] table) {
		int total = 0;
		foreach (Tuple<int, object> item in table) {
			total += item.Item1;
		}
		int roll = UnityEngine.Random.Range(1, total + 1);
		foreach (Tuple<int, object> item in table) {
			if (roll <= item.Item1) {
				return item.Item2;
			}
			roll -= item.Item1;
		}
		// We shouldn't get here? Just return the first thing.
		Debug.LogError("WeightedRandom ran out of items.");
		return table[0].Item2;
	}

	bool IsGeneratedObject(GameObject obj) {
		Transform parent = obj.transform.parent;
		while (parent) {
			if (parent == generatedObjects.transform) {
				return true;
			}
			parent = parent.parent;
		}
		return false;
	}

	List<Vector3> GeneratePoisson(float width, float height, float minDistance, int tries) {
		var points = new List<Vector3>();
		float cellSize = minDistance / Mathf.Sqrt(2f);
		var grid = new Vector3[Mathf.CeilToInt(width/cellSize), Mathf.CeilToInt(height/cellSize)];
		for (int x = 0; x <= grid.GetUpperBound(0); x++) {
			for (int y = 0; y <= grid.GetUpperBound(1); y++) {
				grid[x, y] = Vector3.positiveInfinity;
			}
		}
		var processPoints = new List<Vector3>();
		var firstPoint = new Vector3(Random.Range(0, width), 0, Random.Range(0, height));
		points.Add(firstPoint);
		processPoints.Add(firstPoint);
		InsertPointIntoGrid(firstPoint, ref grid, cellSize);

		while (processPoints.Count > 0) {
			Vector3 activePoint = processPoints[Random.Range(0, processPoints.Count)];
			bool foundNewPoint = false;
			for (int i = 0; i < tries; i++) {
				var newPoint = GenerateRandomPointNear(activePoint, minDistance);
				if (newPoint.x >= 0f && newPoint.x <= width &&
						newPoint.z >= 0f && newPoint.z <= height &&
						!HasCloseNeighbor(newPoint, ref grid, cellSize, minDistance)) {
					foundNewPoint = true;
					processPoints.Add(newPoint);
					points.Add(newPoint);
					InsertPointIntoGrid(newPoint, ref grid, cellSize);
					break;
				}
			}
			if (!foundNewPoint) {
				processPoints.Remove(activePoint);
			}
		}

		return points;
	}

	Tuple<int, int> GetGridXY (Vector3 point, float cellSize) {
		return Tuple.Create(Mathf.FloorToInt(point.x / cellSize), Mathf.FloorToInt(point.z / cellSize));
	}

	void InsertPointIntoGrid(Vector3 point, ref Vector3[,] grid, float cellSize) {
		var gridXY = GetGridXY(point, cellSize);
		grid[gridXY.Item1, gridXY.Item2] = point;
	}

	Vector3 GenerateRandomPointNear(Vector3 point, float minDistance) {
		float angle = Random.value * 2f * Mathf.PI;
		float distance = Random.Range(minDistance, minDistance * 2f);
		return new Vector3(point.x + distance * Mathf.Cos(angle), 0f, point.z + distance * Mathf.Sin(angle));
	}

	bool HasCloseNeighbor(Vector3 point, ref Vector3[,] grid, float cellSize, float minDistance) {
		var gridXY = GetGridXY(point, cellSize);
		for (int x = Math.Max(0, gridXY.Item1 - 2); x <= Math.Min(grid.GetUpperBound(0), gridXY.Item1 + 2); x++) {
			for (int y = Math.Max(0, gridXY.Item2 - 2); y <= Math.Min(grid.GetUpperBound(1), gridXY.Item2 + 2); y++) {
				if (Vector3.Distance(point, grid[x, y]) < minDistance) {
					return true;
				}
			}
		}
		return false;
	}

	bool CollidesWithSolid(Vector3 point, float maxDistance = Mathf.Infinity) {
		RaycastHit hit;
		var solidLayers = new string[] { "Default", };
		if (Physics.Raycast(point, Vector3.up, out hit, maxDistance, LayerMask.GetMask(solidLayers))) {
			return true;
		}
		return false;
	}

	bool CollidesWithZone(Vector3 point, string zoneName, float maxDistance = Mathf.Infinity) {
		RaycastHit[] hits;
		hits = Physics.RaycastAll(point, Vector3.up, maxDistance, zoneLayer);
		for (int i = 0; i < hits.Length; i++)
		{
			if (hits[i].transform.name == zoneName) {
				return true;
			}
		}
		return false;
	}
}
