using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(SpawnerSystem))]//스폰이 완료된후 업데이트 시작
public partial class EditPosSystem : SystemBase
{
    NativeArray<Entity> spanwed;
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        spanwed = Entities.WithAll<SpawnTargetTag>().ToQuery().ToEntityArray(Allocator.Persistent);
        //EntityManager.CreateEntityQuery(typeof(SpawnTargetTag)).ToEntityArray(Allocator.Persistent);//이렇게도 가능


        ToggleAll(false);

        SpawnController.instance.OnChangeRunning = OnChangeRunning;
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        spanwed.Dispose();
    }
    protected override void OnUpdate()
    {
        if (SpawnController.instance.IsRunning)
        {
            switch (SpawnController.instance.RunType)
            {
                case RunType.Entities:
                    RandomPos_Entities();
                    break;
                case RunType.IJobParral:
                    RandomPos_IJobParrel();
                    break;
                case RunType.IJobEntity:
                    RandomPos_IJobEntity();
                    break;
                case RunType.참조Reference:
                    RandomPos_Ref();
                    break;
            }
        }
    }

    private void OnChangeRunning(bool input)
    {
        ToggleAll(input);
    }

    #region Job
    [BurstCompile]
    public partial struct ToggleAllJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeArray<Entity> spanwed;
        public bool vaule;

        public void Execute(int index)
        {
            ecb.SetEnabled(index, spanwed[index], vaule);
        }
    }

    [BurstCompile]
    public partial struct RandomPosJob_IJobParrel : IJobParallelFor
    {
        public int seed;
        [ReadOnly] public NativeArray<Entity> entities;
        public EntityCommandBuffer.ParallelWriter ecb;
        public Vector3 SpawnArea;
        public void Execute(int index)
        {
            var random = new Unity.Mathematics.Random((uint)Mathf.Max(seed + index, 1));
            random.NextFloat();//왜인지 모르겠지만 항상 최솟값

            ecb.SetComponent(index, entities[index], new LocalTransform
            {
                Position = random.NextFloat3(SpawnArea * -0.5f, SpawnArea * 0.5f),
                Scale = 0.5f
            });
        }
    }
    [BurstCompile]
    public partial struct RandomPosJob_IJobEntity : IJobEntity
    {
        public int seed;
        public EntityCommandBuffer.ParallelWriter ecb;
        public Vector3 SpawnArea;

        public void Execute([EntityIndexInQuery]int index , Entity entity, in SpawnTargetTag tag)
        {
            var random = new Unity.Mathematics.Random((uint)Mathf.Max(seed + index, 1));
            random.NextFloat();//왜인지 모르겠지만 항상 최솟값

            ecb.SetComponent(index, entity, new LocalTransform
            {
                Position = random.NextFloat3(SpawnArea * -0.5f, SpawnArea * 0.5f),
                Scale = 0.5f
            });
        }
    }
    [BurstCompile]
    public partial struct RandomPosJob_Ref : IJobEntity
    {
        public int seed;
        public Vector3 SpawnArea;
        public void Execute([EntityIndexInQuery] int index, Entity entity, in SpawnTargetTag tag, ref LocalTransform transform)
        {
            var random = new Unity.Mathematics.Random((uint)Mathf.Max(seed + index, 1));
            random.NextFloat();//왜인지 모르겠지만 항상 최솟값

            transform.Position = random.NextFloat3(SpawnArea * -0.5f, SpawnArea * 0.5f);
        }
    }

    #endregion job

    #region Fuction

    public void ToggleAll(bool vaule)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        new ToggleAllJob()
        {
            ecb = ecb.AsParallelWriter(),
            spanwed = spanwed,
            vaule = vaule
        }.Schedule(spanwed.Length, 64, Dependency).Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
    public void RandomPos_Entities()
    {

        Entities.WithAll<SpawnTargetTag>().ForEach((int entityInQueryIndex, ref LocalTransform transform)=>
        {
            var seed = (int)(SystemAPI.Time.ElapsedTime % 1000 * 1000) + entityInQueryIndex;
            var random = new Unity.Mathematics.Random((uint)Mathf.Max(seed, 1));

            random.NextFloat();//왜인지 모르겠지만 항상 최솟값
            transform.Position = random.NextFloat3(new float3(1, 1, 1) * -2.5f, new float3(1, 1, 1) * 2.5f);
            //random.NextFloat3(SpawnController.instance.SpawnArea * -0.5f, SpawnController.instance.SpawnArea * 0.5f);
            // 되긴하지만 에러뜸 (The managed class type `SpawnController` is not supported.)
        }).ScheduleParallel(Dependency).Complete();

        //.Run() 이 아니면 외부 변수 사용 불가
    }
    public void RandomPos_IJobParrel()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        new RandomPosJob_IJobParrel()
        {
            ecb = ecb.AsParallelWriter(),
            entities = spanwed,
            seed = (int)(SystemAPI.Time.ElapsedTime % 1000 * 1000),
            SpawnArea = SpawnController.instance.SpawnArea
        }.Schedule(spanwed.Length, 64, Dependency).Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
    public void RandomPos_IJobEntity()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        new RandomPosJob_IJobEntity()
        {
            ecb = ecb.AsParallelWriter(),
            seed = (int)(SystemAPI.Time.ElapsedTime % 1000 * 1000),
            SpawnArea = SpawnController.instance.SpawnArea
        }.ScheduleParallel(Dependency).Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
    public void RandomPos_Ref()
    {
        new RandomPosJob_Ref()
        {
            seed = (int)(SystemAPI.Time.ElapsedTime % 1000 * 1000),
            SpawnArea = SpawnController.instance.SpawnArea
        }.ScheduleParallel(Dependency).Complete();
    }
    #endregion
}
