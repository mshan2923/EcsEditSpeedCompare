using System.Collections;
using System.Collections.Generic;
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
            //SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged); // EditPosSystem�� ����ɶ� ������ ���� ����
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


    public partial struct SpawnAndDisableJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public SpawnComponent spawn;

        public void Execute(int index)
        {
            
            var entity = ecb.Instantiate(index, spawn.SpawnEntity);
            //ecb.SetEnabled(index, entity, false);
            ecb.SetComponent(index, entity, new LocalTransform 
            { 
                Position = new Unity.Mathematics.float3(1, 1, 1) * index * 0.1f ,
                Scale = 0.5f
            });
            ecb.AddComponent(index, entity, new SpawnTargetTag());
        }
    }
}
