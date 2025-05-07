using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SplitLooseMeshes;

public class SplitLooseMeshes : ResoniteMod
{
    public override string Name => "SplitLooseMeshes";
    public override string Author => "dfgHiatus";
    public override string Version => "2.0.0";
    public override string Link => "https://github.com/dfgHiatus/SplitLooseMeshes/";
    public override void OnEngineInit()
    {
        new Harmony("net.dfgHiatus.SplitLooseMeshes").PatchAll();
        config = GetConfiguration();
    }

    private static ModConfiguration config;

    [AutoRegisterConfigKey]
    private static ModConfigurationKey<bool> mergeDoubles = new ModConfigurationKey<bool>("merge_Doubles", "Merge doubles before separating. May help in cases where separations are too aggressive", () => false);

    [AutoRegisterConfigKey]
    private static ModConfigurationKey<double> mergeDoublesCellSize = new ModConfigurationKey<double>("merge_Doubles_Cell_Size", "Cell size for above remove doubles operation", () => 0.001);

    [AutoRegisterConfigKey]
    private static ModConfigurationKey<bool> destroyOriginal = new ModConfigurationKey<bool>("destroy_Original", "Remove the original mesh renderer on completion", () => true);
    

    [HarmonyPatch(typeof(MeshRenderer), "BuildInspectorUI", typeof(UIBuilder))]
    class SplitLooseMeshesPatch
    {
        public static void Postfix(MeshRenderer __instance, UIBuilder ui)
        {
            var button = ui.Button("Split by loose parts");

            button.LocalPressed += (sender, data) =>
            {
                RunProcessingTask(__instance, button, SplitLoosePartsTask);
            };
        }
        
        private static async Task SplitLoosePartsTask(MeshX sourceMesh, Slot assetTarget, MeshRenderer __instance)
        {
            List<int> materialIndices;
            var splitMeshes = sourceMesh.SplitByLooseParts(out materialIndices, config.GetValue(mergeDoubles), config.GetValue(mergeDoublesCellSize));

            foreach (MeshX item in splitMeshes)
            {
                PostprocessSplitMesh(item);
            }
            
            List<Uri> savedUris = new List<Uri>();
            foreach (MeshX meshes in splitMeshes)
            {
                List<Uri> list = savedUris;
                list.Add(await Engine.Current.LocalDB.SaveAssetAsync(meshes).ConfigureAwait(continueOnCapturedContext: false));
            }
            
            await default(ToWorld);
            MeshCollider component = __instance.Slot.GetComponent((MeshCollider c) => c.Mesh.Target == __instance.Mesh.Target);
            for (int i = 0; i < savedUris.Count; i++)
            {
                Slot slot = __instance.Slot.AddSlot("Loose mesh #" + i);
                StaticMesh target = assetTarget.AttachStaticMesh(savedUris[i]);
                MeshRenderer meshRenderer = AttachSplitMesh(slot);
                meshRenderer.Mesh.Target = target;
                meshRenderer.Material.Target = __instance.Materials[materialIndices[i]];
                if (component != null)
                {
                    MeshCollider meshCollider = slot.AttachComponent<MeshCollider>();
                    meshCollider.Mesh.Target = target;
                    meshCollider.Type.Value = component.Type.Value;
                    meshCollider.CharacterCollider.Value = component.CharacterCollider.Value;
                    meshCollider.IgnoreRaycasts.Value = component.IgnoreRaycasts.Value;
                }
            }

            if (config.GetValue(destroyOriginal))
            {
                component?.Destroy();
                __instance.Destroy();                    
            }
        }

        // Copied over
        private static MeshRenderer AttachSplitMesh(Slot root)
        {
            return root.AttachComponent<MeshRenderer>();
        } 

        // Copied over
        private static void PostprocessSplitMesh(MeshX mesh)
        {
            mesh.StripEmptyBlendshapes();
        }

        // Copied over
        private static Task RunProcessingTask(MeshRenderer __instance, IButton button, Func<MeshX, Slot, MeshRenderer, Task> process)
        {
            if (__instance.Mesh.Asset == null)
            {
                return Task.CompletedTask;
            }
            
            if (button != null)
            {
                button.LabelText = __instance.GetLocalized("General.Processing");
                button.Enabled = false;
            }
            
            Slot assetTarget = __instance.Slot;
            if (__instance.Mesh.Target.Slot != __instance.Slot)
            {
                assetTarget = __instance.World.AssetsSlot.AddSlot(__instance.Slot.Name + " - Processed");
            }

            return __instance.StartTask(async delegate
            {
                await default(ToBackground);
                object _lock = new object();
                await __instance.Mesh.Asset.RequestReadLock(_lock).ConfigureAwait(continueOnCapturedContext: false);
                MeshX arg = new MeshX(__instance.Mesh.Asset.Data);
                __instance.Mesh.Asset.ReleaseReadLock(_lock);
                try
                {
                    await process(arg, assetTarget, __instance);
                    await default(ToWorld);
                    if (button != null)
                    {
                        button.LabelText = __instance.GetLocalized("General.Done");
                        button.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    UniLog.Error("Exception processing mesh:\n" + ex, stackTrace: false);
                    await default(ToWorld);
                    if (button != null)
                    {
                        button.LabelText = __instance.GetLocalized("General.FAILED");
                        button.Enabled = true;
                    }
                }
            });
        }     
    }
}