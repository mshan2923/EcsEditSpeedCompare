using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public enum RunType { Entities, IJobParral, IJobEntity, ÂüÁ¶Reference}

public class SpawnController : MonoBehaviour
{
    public static SpawnController instance;
    public int SpawnAmount = 10000;
    public GameObject SpawnPrefab;
    public Vector3 SpawnArea = Vector3.one;

    public RunType RunType;

    public delegate void SpawnAction(bool input);
    public SpawnAction OnChangeRunning;

    private bool isRunning = false;
    public bool IsRunning
    {
        get => isRunning;
        set
        {
            if (OnChangeRunning != null)
                OnChangeRunning(value);
            isRunning = value;
        }
    }
}

[CustomEditor(typeof(SpawnController))]
public class SpawnControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();


        if (GUILayout.Button(SpawnController.instance.IsRunning ? " Running" : " Stoped"))
        {
            SpawnController.instance.IsRunning = !SpawnController.instance.IsRunning;
        }
    }
}

public struct SpawnComponent : IComponentData
{
    public Entity SpawnEntity;
}
public struct SpawnTargetTag : IComponentData { }

public class SpawnControllerBaker : Baker<SpawnController>
{
    public override void Bake(SpawnController authoring)
    {
        SpawnController.instance = authoring;

        //if (authoring.SpawnPrefab != null)
        //    authoring.SpawnEntity = GetEntity(authoring.SpawnPrefab, TransformUsageFlags.Dynamic);
        if (authoring.SpawnPrefab != null)
        {
            AddComponent(
                GetEntity(
                    authoring, TransformUsageFlags.Dynamic),
                        new SpawnComponent
                           {
                            SpawnEntity = GetEntity(authoring.SpawnPrefab, TransformUsageFlags.Dynamic)
                           }
                        );
        }

    }
}
