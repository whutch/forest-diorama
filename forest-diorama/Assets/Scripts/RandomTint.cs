using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTint : MonoBehaviour {

	public int materialIndex = 0;
	public string colorProperty = "_Color";

	void Start () {
		ShiftColor();
	}

	public void ShiftColor() {
		Renderer renderer = GetComponent<Renderer>();
		Color color = renderer.materials[materialIndex].GetColor(colorProperty);
		color.r = Mathf.Clamp(color.r + Random.Range(-0.1f, 0.1f), 0f, 1f);
		color.g = Mathf.Clamp(color.g + Random.Range(-0.1f, 0.1f), 0f, 1f);
		color.b = Mathf.Clamp(color.b + Random.Range(-0.1f, 0.1f), 0f, 1f);
		renderer.materials[materialIndex].SetColor(colorProperty, color);
	}
}
