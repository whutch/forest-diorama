using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(Generator))]
public class GeneratorEditor : Editor {

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		Generator generator = (Generator) target;
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Generate")) {
			generator.GenerateDiorama();
		}
		if (GUILayout.Button("New Seed")) {
			generator.NewSeed();
			generator.GenerateDiorama();
		}
		GUILayout.EndHorizontal();
		if (GUILayout.Button("Camera Cycle")) {
			generator.CameraGo();
		}
	}
}
