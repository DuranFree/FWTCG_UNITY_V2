using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FWTCG.VFX;
using FWTCG.FX;
using FWTCG.Data;
using FWTCG.UI;

namespace FWTCG.Tests
{
    /// <summary>VFX-8 EditMode tests: Projectile system.</summary>
    [TestFixture]
    public class VFX8ProjectileTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // Projectile constants
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Projectile_DefaultDuration_Is04()
        {
            Assert.AreEqual(0.4f, Projectile.DEFAULT_DURATION, 0.001f);
        }

        [Test]
        public void Projectile_ArcHeightRatio_Is03()
        {
            Assert.AreEqual(0.3f, Projectile.ARC_HEIGHT_RATIO, 0.001f);
        }

        [Test]
        public void Projectile_MinDuration_Is005()
        {
            Assert.AreEqual(0.05f, Projectile.MIN_DURATION, 0.001f);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Projectile.Init
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Projectile_Init_StoresStartEnd()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject("ProjTest");
            var proj = go.AddComponent<Projectile>();
            Vector3 start = new Vector3(-100, -200, 0);
            Vector3 end = new Vector3(100, 200, 0);
            proj.Init(start, end, 0.5f);

            Assert.AreEqual(start, proj.StartPos);
            Assert.AreEqual(end, proj.EndPos);
            Assert.AreEqual(0.5f, proj.Duration, 0.001f);
            Assert.IsTrue(proj.IsRunning);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Projectile_Init_ClampsMinDuration()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject("ProjTest");
            var proj = go.AddComponent<Projectile>();
            proj.Init(Vector3.zero, Vector3.one * 100, 0.001f); // below MIN

            Assert.AreEqual(Projectile.MIN_DURATION, proj.Duration, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Projectile_Init_ControlPointAboveMidpoint()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject("ProjTest");
            var proj = go.AddComponent<Projectile>();
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 end = new Vector3(200, 0, 0);
            proj.Init(start, end, 0.5f);

            // Control point should be at midpoint + upward offset
            Vector3 mid = (start + end) * 0.5f;
            Assert.Greater(proj.ControlPt.y, mid.y, "Control point Y should be above midpoint for arc");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Projectile_Init_PositionAtStart()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject("ProjTest");
            var proj = go.AddComponent<Projectile>();
            Vector3 start = new Vector3(-50, -100, 0);
            proj.Init(start, new Vector3(50, 100, 0), 0.5f);

            Assert.AreEqual(start.x, go.transform.position.x, 0.1f);
            Assert.AreEqual(start.y, go.transform.position.y, 0.1f);

            Object.DestroyImmediate(go);
        }

        // ══════════════════════════════════════════════════════════════════════
        // FXTool.DoProjectileFX
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void FXTool_DoProjectileFX_NullPrefab_ReturnsNull()
        {
            var result = FXTool.DoProjectileFX(null, Vector3.zero, Vector3.one);
            Assert.IsNull(result);
        }

        [Test]
        public void FXTool_DoProjectileFX_CreatesGOWithProjectile()
        {
            LogAssert.ignoreFailingMessages = true;
            var prefab = new GameObject("FXPrefab");
            var go = FXTool.DoProjectileFX(prefab, Vector3.zero, new Vector3(100, 0, 0), 0.5f);

            Assert.IsNotNull(go);
            var proj = go.GetComponent<Projectile>();
            Assert.IsNotNull(proj, "Should have Projectile component");
            Assert.IsTrue(proj.IsRunning);
            Assert.AreEqual(0.5f, proj.Duration, 0.001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void FXTool_DoProjectileFX_CallbackStored()
        {
            LogAssert.ignoreFailingMessages = true;
            var prefab = new GameObject("FXPrefab");
            bool called = false;
            var go = FXTool.DoProjectileFX(prefab, Vector3.zero, Vector3.right * 100, 0.3f, () => called = true);

            Assert.IsNotNull(go);
            // Callback not yet called (hasn't arrived)
            Assert.IsFalse(called, "Callback should not fire immediately");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(prefab);
        }

        // ══════════════════════════════════════════════════════════════════════
        // VFXResolver.ResolveProjectile
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void ResolveProjectile_NullCard_ReturnsNull()
        {
            Assert.IsNull(VFXResolver.ResolveProjectile(null));
        }

        [Test]
        public void ResolveProjectile_SpellCard_ReturnsFXByRuneType()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            // Use reflection to set private fields for test
            SetPrivateField(card, "_isSpell", true);
            SetPrivateField(card, "_runeType", RuneType.Blazing);

            string result = VFXResolver.ResolveProjectile(card);
            Assert.AreEqual(VFXResolver.FX_FLAME, result);

            Object.DestroyImmediate(card);
        }

        [Test]
        public void ResolveProjectile_EquipmentCard_ReturnsFXByRuneType()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_isEquipment", true);
            SetPrivateField(card, "_runeType", RuneType.Order);

            string result = VFXResolver.ResolveProjectile(card);
            Assert.AreEqual(VFXResolver.FX_WATER, result);

            Object.DestroyImmediate(card);
        }

        [Test]
        public void ResolveProjectile_UnitCard_ReturnsNull()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            // Default: not spell, not equipment → unit
            SetPrivateField(card, "_runeType", RuneType.Blazing);

            string result = VFXResolver.ResolveProjectile(card);
            Assert.IsNull(result, "Unit cards should not have projectiles");

            Object.DestroyImmediate(card);
        }

        [Test]
        public void ResolveProjectile_EffectIdOverride_HexRay()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_isSpell", true);
            SetPrivateField(card, "_effectId", "hex_ray");

            string result = VFXResolver.ResolveProjectile(card);
            Assert.AreEqual(VFXResolver.FX_FLAME, result);

            Object.DestroyImmediate(card);
        }

        [Test]
        public void ResolveProjectile_EffectIdOverride_Slam()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_isSpell", true);
            SetPrivateField(card, "_effectId", "slam");

            string result = VFXResolver.ResolveProjectile(card);
            Assert.AreEqual(VFXResolver.FX_ELECTRIC, result);

            Object.DestroyImmediate(card);
        }

        // ══════════════════════════════════════════════════════════════════════
        // VFXResolver.GetProjectileFXName — all RuneTypes
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void GetProjectileFXName_AllRuneTypes_NonNull()
        {
            foreach (RuneType rt in System.Enum.GetValues(typeof(RuneType)))
            {
                string result = VFXResolver.GetProjectileFXName(rt);
                Assert.IsNotNull(result, $"RuneType {rt} should have a projectile FX name");
            }
        }

        [Test]
        public void GetProjectileFXName_Blazing_IsFlame()
        {
            Assert.AreEqual(VFXResolver.FX_FLAME, VFXResolver.GetProjectileFXName(RuneType.Blazing));
        }

        [Test]
        public void GetProjectileFXName_Radiant_IsRayGlow()
        {
            Assert.AreEqual(VFXResolver.FX_RAYGLOW, VFXResolver.GetProjectileFXName(RuneType.Radiant));
        }

        [Test]
        public void GetProjectileFXName_Verdant_IsLeaf()
        {
            Assert.AreEqual(VFXResolver.FX_LEAF, VFXResolver.GetProjectileFXName(RuneType.Verdant));
        }

        [Test]
        public void GetProjectileFXName_Crushing_IsHit()
        {
            Assert.AreEqual(VFXResolver.FX_HIT, VFXResolver.GetProjectileFXName(RuneType.Crushing));
        }

        [Test]
        public void GetProjectileFXName_Chaos_IsCast()
        {
            Assert.AreEqual(VFXResolver.FX_CAST, VFXResolver.GetProjectileFXName(RuneType.Chaos));
        }

        [Test]
        public void GetProjectileFXName_Order_IsWater()
        {
            Assert.AreEqual(VFXResolver.FX_WATER, VFXResolver.GetProjectileFXName(RuneType.Order));
        }

        // ══════════════════════════════════════════════════════════════════════
        // SpellVFX constants
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void SpellVFX_ProjectileDuration_Matches()
        {
            Assert.AreEqual(0.4f, SpellVFX.PROJECTILE_DURATION, 0.001f);
        }

        [Test]
        public void SpellVFX_PlayerHandY_Negative()
        {
            Assert.Less(SpellVFX.PLAYER_HAND_Y, 0f, "Player hand origin should be below center");
        }

        [Test]
        public void SpellVFX_AIOriginY_Positive()
        {
            Assert.Greater(SpellVFX.AI_ORIGIN_Y, 0f, "AI origin should be above center");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Edge cases
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Projectile_ZeroDist_NoException()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject("ProjTest");
            var proj = go.AddComponent<Projectile>();
            // Start == End → zero distance
            Assert.DoesNotThrow(() => proj.Init(Vector3.zero, Vector3.zero, 0.3f));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Projectile_NegativeDuration_ClampsToMin()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject("ProjTest");
            var proj = go.AddComponent<Projectile>();
            proj.Init(Vector3.zero, Vector3.right * 100, -1f);

            Assert.AreEqual(Projectile.MIN_DURATION, proj.Duration, 0.001f);
            Object.DestroyImmediate(go);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Helper
        // ══════════════════════════════════════════════════════════════════════

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(obj, value);
        }
    }
}
