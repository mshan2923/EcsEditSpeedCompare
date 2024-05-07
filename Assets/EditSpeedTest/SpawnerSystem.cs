using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;


public partial class SpawnerSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
            //SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged); // EditPosSystem이 실행될때 스폰이 되지 않음
        var spawnHandle = new SpawnAndDisableJob()
        {
            ecb = ecb.AsParallelWriter(),
            spawn = SystemAPI.GetSingleton<SpawnComponent>()
        }.Schedule(SpawnController.instance.SpawnAmount, 64, Dependency);
        spawnHandle.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
    protected override void OnUpdate()
    {
        Enabled = false;
    }

    [BurstCompile]
    public partial struct SpawnAndDisableJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public SpawnComponent spawn;

        public void Execute(int index)
        {
            
            var entity = ecb.Instantiate(index, spawn.SpawnEntity);
            ecb.SetComponent(index, entity, new LocalTransform 
            { 
                Position = Unity.Mathematics.float3.zero ,
                Scale = 0.5f
            });
            ecb.AddComponent(index, entity, new SpawnTargetTag());

            //아직 스폰되지 않은 상태이여서 , entity의 인덱스가 음수값으로 나와요.
        }
    }
}
