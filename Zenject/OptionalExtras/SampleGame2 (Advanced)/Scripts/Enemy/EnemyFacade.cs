﻿using System;
using UnityEngine;
using Zenject;

namespace Zenject.SpaceFighter
{
    public class EnemyFacade : MonoBehaviour
    {
        Enemy _enemy;
        EnemyTunables _tunables;
        EnemyDeathHandler _deathHandler;

        // We can't use a constructor here because MonoFacade derives from
        // MonoBehaviour
        [Inject]
        public void Construct(
            Enemy enemy, EnemyTunables tunables,
            EnemyDeathHandler deathHandler)
        {
            _enemy = enemy;
            _tunables = tunables;
            _deathHandler = deathHandler;
        }

        // Here we can add some high-level methods to give some info to other
        // parts of the codebase outside of our enemy facade
        public Vector3 Position
        {
            get { return _enemy.Position; }
            set { _enemy.Position = value; }
        }

        public void Update()
        {
            // Always ensure we are on the main plane
            _enemy.Position = new Vector3(_enemy.Position.x, _enemy.Position.y, 0);
        }

        public void Die()
        {
            _deathHandler.Die();
        }

        public class Pool : MonoMemoryPool<EnemyTunables, EnemyFacade>
        {
            protected override void Reinitialize(EnemyFacade enemy, EnemyTunables tunables)
            {
                enemy._tunables.Accuracy = tunables.Accuracy;
                enemy._tunables.Speed = tunables.Speed;
            }
        }
    }
}
