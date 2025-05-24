using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletScriptVM;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    /// <summary>
    /// This job is responsible for implementing the <see cref="Command"/>s the
    /// <see cref="VM"/>s output.
    /// <br/>
    /// To guarantee determinism, this job is synchronous over <i>all</i> VMs.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal struct VMsCommandsJob : IJob {

        public NativeList<VM> vms;
        public BulletField field;
        public NativeReference<Player> player;
        // See the docs -- these are the bullets AddScript etc add to.
        public NativeList<BulletReference> createdBullets;


        public unsafe void Execute() {
            VMCommandsPass.Execute(in vms, in player, ref field, ref createdBullets);
        }
    }

    public static class VMCommandsPass {
        public static unsafe void Execute(
            in NativeList<VM> vms,
            in NativeReference<Player> player,
            ref BulletField field,
            ref NativeList<BulletReference> createdBullets
        ) {
            float2 bulletSpawnPos = 0;
            float bulletSpawnRot = 0;

            // In general, the underlying pointer for lists may move around.
            // Luckily, we do not resize this list during this, so that is not
            // a problem.
            var ptr = vms.GetUnsafeTypedPtr();
            for (int i = 0; i < vms.Length; i++) {
                var vm = ptr + i;
                createdBullets.Clear();
                foreach (var command in vm->outputCommands) {
                    RunCommand(
                        in vm,
                        in command,
                        player.Value,
                        ref bulletSpawnPos,
                        ref bulletSpawnRot,
                        ref createdBullets,
                        ref field
                    );
                }
                vm->outputCommands.Clear();
            }
        }

        static unsafe void RunCommand(
            in VM* vm,
            in Command command,
            in Player player,
            ref float2 bulletSpawnPos,
            ref float bulletSpawnRot,
            ref NativeList<BulletReference> createdBullets,
            ref BulletField field
        ) {
            switch (command.command) {
                case CommandEnum.ContinueAfterTime:
                    // The cooldown is stored in the top half of the VM's OP.
                    // This command clamps the waittime to [1,65535].
                    ushort cooldown = (ushort)math.clamp(command.arg1, 1, 65535);
                    vm->SetCooldown(cooldown);
                    break;
                case CommandEnum.SendMessage:
                    throw new NotImplementedException();
                case CommandEnum.SpawnPosRot:
                    bulletSpawnPos = new(command.arg1, command.arg2);
                    bulletSpawnRot = command.arg3;
                    break;
                case CommandEnum.Spawn:
                    RunSpawnCommand(
                        in command,
                        ref bulletSpawnPos,
                        ref bulletSpawnRot,
                        ref createdBullets,
                        ref field
                    );
                    break;
                case CommandEnum.Destroy:
                    throw new NotImplementedException();
                case CommandEnum.LoadBackground:
                    throw new NotImplementedException();
                case CommandEnum.AddScriptBarrier:
                    createdBullets.Clear();
                    break;
                case CommandEnum.AddScript:
                case CommandEnum.AddScriptWithValue:
                    throw new NotImplementedException();
                case CommandEnum.StartScript:
                case CommandEnum.StartScriptWithValue:
                case CommandEnum.StartScriptMany:
                case CommandEnum.StartScriptManyWithValue:
                    throw new NotImplementedException();
                case CommandEnum.Depivot:
                    throw new NotImplementedException();
                case CommandEnum.AddRotation:
                    AddRotationCommand(vm, command, ref field);
                    break;
                case CommandEnum.SetRotation:
                    SetRotationCommand(vm, command, ref field);
                    break;
                case CommandEnum.AddSpeed:
                    AddSpeedCommand(vm, command, ref field);
                    break;
                case CommandEnum.SetSpeed:
                    SetSpeedCommand(vm, command, ref field);
                    break;
                case CommandEnum.FacePlayer:
                    FacePlayerCommand(vm, command, player, ref field);
                    break;
                case CommandEnum.Gimmick:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException($"Unknown/unsupported command:\n{command}");
            }
        }

        static unsafe void RunSpawnCommand(
            in Command command,
            ref float2 bulletSpawnPos,
            ref float bulletSpawnRot,
            ref NativeList<BulletReference> createdBullets,
            ref BulletField field
        ) {
            // TODO: Actually pass the bullet properties.
            var spawnSpeed = command.arg1;
            var bulletTypeIndex = command.arg2;
            // Our angles are [0,1] clockwise instead of [0,2π] counterclockwise, with 0 = down.
            math.sincos(-2 * math.PI * (bulletSpawnRot - 0.25f), out float s, out float c);
            BulletCreationParams props = new(
                spawnPosition: bulletSpawnPos,
                movement: new float2(spawnSpeed * c, spawnSpeed * s),
                // TODO: Do everything below using `bulletTypeIndex` and bullet style sheets.
                hitboxSize: 0.03f,
                textureID: 0,
                layer: 0,
                outerColor: new(0, 0, 1, 1),
                innerColor: new(1, 1, 1, 1),
                bulletProps: MiscBulletProps.HarmsPlayers | MiscBulletProps.ClearOnEdge,
                renderScale: 1f
            );

            var bref = field.CreateBullet(ref props);
            if (!bref.HasValue)
                return;
            createdBullets.Add(bref.Value);
        }

        static unsafe void AddRotationCommand(
            in VM* vm,
            in Command command,
            ref BulletField field
        ) {
            foreach (var bref in vm->affectedBullets) {
                int i = (int)bref;
                var dx = field.dx[i];
                var dy = field.dy[i];
                var r = math.length(new float2(dx, dy));
                var angle = math.atan2(dy, dx);
                // Our angles are [0,1] clockwise instead of [0,2π] counterclockwise
                angle -= 2 * math.PI * command.arg1;
                math.sincos(angle, out float s, out float c);
                field.dx[i] = r * c;
                field.dy[i] = r * s;
            }
        }

        static unsafe void SetRotationCommand(
            in VM* vm,
            in Command command,
            ref BulletField field
        ) {
            foreach (var bref in vm->affectedBullets) {
                int i = (int)bref;
                var dx = field.dx[i];
                var dy = field.dy[i];
                var r = math.length(new float2(dx, dy));
                // Our angles are [0,1] clockwise instead of [0,2π] counterclockwise
                var angle = -2 * math.PI * command.arg1;
                math.sincos(angle, out float s, out float c);
                field.dx[i] = r * c;
                field.dy[i] = r * s;
            }
        }

        static unsafe void AddSpeedCommand(
            in VM* vm,
            in Command command,
            ref BulletField field
        ) {
            foreach (var bref in vm->affectedBullets) {
                int i = (int)bref;
                var dx = field.dx[i];
                var dy = field.dy[i];
                // The argument is in "per second", but stored is "per tick"
                // TODO: Speed 0 is whack.
                var oldSpeed = 60 * math.length(new float2(dx, dy));
                var factor = (oldSpeed + command.arg1) / oldSpeed;
                field.dx[i] *= factor;
                field.dy[i] *= factor;
            }
        }

        static unsafe void SetSpeedCommand(
            in VM* vm,
            in Command command,
            ref BulletField field
        ) {
            foreach (var bref in vm->affectedBullets) {
                int i = (int)bref;
                var dx = field.dx[i];
                var dy = field.dy[i];
                // The argument is in "per second", but stored is "per tick".
                var oldSpeed = 60 * math.length(new float2(dx, dy));
                var factor = command.arg1 / oldSpeed;
                field.dx[i] *= factor;
                field.dy[i] *= factor;
            }
        }

        static unsafe void FacePlayerCommand(
            in VM* vm,
            in Command command,
            in Player player,
            ref BulletField field
        ) {
            foreach (var bref in vm->affectedBullets) {
                int i = (int)bref;
                var playerPos = player.position;
                var x = field.x[i];
                var y = field.y[i];
                var dx = field.dx[i];
                var dy = field.dy[i];
                var r = math.length(new float2(dx, dy));
                var angle = math.atan2(playerPos.y - y, playerPos.x - x);
                math.sincos(angle, out float s, out float c);
                field.dx[i] = r * c;
                field.dy[i] = r * s;
            }
        }
    }
}
