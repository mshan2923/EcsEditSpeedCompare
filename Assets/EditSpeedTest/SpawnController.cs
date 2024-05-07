using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public enum RunType { Entities, IJobParral, IJobEntity, 참조Reference}

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

    public float AutoStopSecond = 5f;
    [HideInInspector] public float counter = 0;

    public IEnumerator TimerCoroutine(float vaule)
    {
        counter = 0;
        float AvgDelta = Time.deltaTime;
        while ((counter <= vaule) && IsRunning)
        {
            counter += Time.deltaTime;
            AvgDelta = (AvgDelta + Time.deltaTime) * 0.5f; 
            yield return null;
        }
        Debug.Log($"{RunType} : Average FPS : {1 / AvgDelta}");

        IsRunning = false;
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

        if (GUILayout.Button(SpawnController.instance.IsRunning ? $" (Running) {(SpawnController.instance.AutoStopSecond - SpawnController.instance.counter):0.#}s Left" : " (Stoped)Auto Stop"))
        {
            SpawnController.instance.IsRunning = !SpawnController.instance.IsRunning;
            SpawnController.instance.StartCoroutine(SpawnController.instance.TimerCoroutine(SpawnController.instance.AutoStopSecond));
        }
    }
}

public struct SpawnComponent : IComponentData
{
    public Entity SpawnEntity;
}
public struct SpawnTargetTag : IComponentData { }//추가해도 성능지장 없음 , 단순 구별 용도

public class SpawnControllerBaker : Baker<SpawnController>
{
    public override void Bake(SpawnController authoring)
    {
        SpawnController.instance = authoring;

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
