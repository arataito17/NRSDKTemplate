using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NRKernal.Experimental.Editor
{
    public class CoastersTrackingInstaller
    {
        #region settings
        const string NRPluginsContent = "{\r\n" +
            "    \"perception\": {\r\n" +
            "        \"versions\": \"diE1v4iRCoXc8G5g\",\r\n" +
            "        \"plugins_64\": [\r\n" +
            "            {\r\n" +
            "                \"id\": \"nr_image_tracking_id\",\r\n" +
            "                \"path\": \"libnr_image_tracking.so\"\r\n" +
            "            }\r\n" +
            "        ]\r\n" +
            "    }\r\n" +
            "}";

        static string coastersTrackingInstallDir => System.IO.Path.GetFullPath("Assets/NRSDKExperimental/CoastersTrackingSetup/");
        public static string coastersTrackingDemoDir => System.IO.Path.GetFullPath("Assets/NRSDKExperimental/Demos/CoastersImageTracking");
        public static string coastersTrackingSetupZipFile => $"{coastersTrackingInstallDir}setup.zip"; 
        public static string coastersTrackingAARZipFile => $"{coastersTrackingInstallDir}aars.zip";
        static string streamingAssetsDir => System.IO.Path.GetFullPath("Assets/StreamingAssets/");

        static string normal_aar_path => System.IO.Path.GetFullPath("Assets/NRSDK/Plugins/Android/nr_image_tracking.aar");
        public static string coasters_aar_path => $"{coastersTrackingDemoDir}/Plugins/Android/nr_image_tracking.aar";
        static string backup_normal_aar_path => $"{coastersTrackingInstallDir}orig_aars~/nr_image_tracking.aar";
        static string backup_coasters_aar_path => $"{coastersTrackingInstallDir}aars~/nr_image_tracking.aar";

        #endregion

        [MenuItem("NRSDK/CoastersTrackingModule/Install",true)]
        public static bool CanInstallCoastersTrackingModule()
        {
            return !System.IO.Directory.Exists(coastersTrackingDemoDir);
        }
        [MenuItem("NRSDK/CoastersTrackingModule/Install")]
        public static void InstallCoastersTrackingModule()
        {
            NRDebugger.Info(coastersTrackingSetupZipFile);
            ZipUtility.UnzipFile(coastersTrackingSetupZipFile, coastersTrackingDemoDir);
            ZipUtility.UnzipFile(coastersTrackingAARZipFile, coastersTrackingInstallDir);


            // write nr_plugins.json to StreamingAssets Folder
            if (!System.IO.Directory.Exists(streamingAssetsDir))
            {
                System.IO.Directory.CreateDirectory(streamingAssetsDir);
            }
            System.IO.File.WriteAllText($"{streamingAssetsDir}/nr_plugins.json", NRPluginsContent);
            // move normal nr_image_tracking.aar to backup dir
            System.IO.File.Move(normal_aar_path, backup_normal_aar_path);
            System.IO.File.Move(normal_aar_path+".meta", backup_normal_aar_path+".meta");
            // copy coasters version nr_image_tracking.aar to place
            System.IO.File.Copy(backup_coasters_aar_path, coasters_aar_path);

            AssetDatabase.Refresh();
        }

        [MenuItem("NRSDK/CoastersTrackingModule/Uninstall",true)]
        public static bool CanUninstallCoastersTrackingModule()
        {
            return System.IO.Directory.Exists(coastersTrackingDemoDir);
        }

        [MenuItem("NRSDK/CoastersTrackingModule/Uninstall")]
        public static void UninstallCoastersTrackingModule()
        {
            System.IO.Directory.Delete(coastersTrackingDemoDir, true);
            System.IO.File.Delete($"{coastersTrackingDemoDir}.meta");

            // delete nr_plugins.json from StreamingAssets Folder
            System.IO.File.Delete($"{streamingAssetsDir}nr_plugins.json");
            // move normal nr_image_tracking.aar back to place
            System.IO.File.Move(backup_normal_aar_path, normal_aar_path);
            System.IO.File.Move($"{backup_normal_aar_path}.meta", $"{normal_aar_path}.meta");

            System.IO.Directory.Delete($"{coastersTrackingInstallDir}aars~/", true);
            System.IO.Directory.Delete($"{coastersTrackingInstallDir}orig_aars~/", true);

            AssetDatabase.Refresh();
        }
    }
}