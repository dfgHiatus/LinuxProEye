using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Tobii.StreamEngine;

namespace LinuxProEye.Interface
{
    public class LinuxProEyeInterface
    {
        // Device, API, etc.
        private static IntPtr deviceContext;
        private static IntPtr apiContext;
        public static GazeData gazeData;

        // Begin Structs/Enums
        [StructLayout(LayoutKind.Sequential)]
        public struct GazeData
        {
            public Eye leftEye;
            public Eye rightEye;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Eye
        {
            public Validity validity;
            public Vector origin;
            public Vector direction;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vector
        {
            public double x;
            public double y;
            public double z;
        }

        public enum Validity
        {
            Invalid = 0,
            Valid = 1
        }
        // End Structs/Enums

        public bool Init()
        {
            // Create API context
            var Error_T = Interop.tobii_api_create(out apiContext, null);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: Could not create API Context for the Tobii Stream Engine");
                return false;
            }

            // Enumerate devices to find connected eye trackers
            List<string> urls;
            Error_T = Interop.tobii_enumerate_local_device_urls(apiContext, out urls);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR) || urls.Count == 0)
            {
                Console.WriteLine("Error: No device found");
                Console.WriteLine(Error_T);
                return false;
            }

            // Connect to the first VPE found
            foreach (string url in urls)
            {
                Error_T = Interop.tobii_device_create(apiContext, url, Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out deviceContext);
                tobii_device_info_t canidateInfo;
                string canidateName;
                Interop.tobii_get_device_info(deviceContext, out canidateInfo);
                Interop.tobii_get_device_name(deviceContext, out canidateName);

                if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
                {
                    Console.WriteLine("Error: Could not connect to the Vive Pro Eye");
                    Console.WriteLine(Error_T);
                    return false;
                }
                else if (!canidateInfo.integration_type.Equals("HMD")) // Non HMD...
                {
                    Console.WriteLine($"Iterating over connected devices, passed {canidateName}");
                    Error_T = Interop.tobii_device_destroy(deviceContext);
                    if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
                    {
                        Console.WriteLine("Something bad happened while iterating devices.");
                        Console.WriteLine(Error_T.ToString());
                    }
                }
                else
                {
                    // If we get here, we have a valid device!
                    break;
                }
            }

            return true;
        }
        private void OnWearable(ref tobii_wearable_consumer_data_t data, IntPtr user_data)
        {
            Console.WriteLine("memes");
        }

        // https://developer.tobiipro.com/commonconcepts/coordinatesystems.html
        private void OnPosition(ref tobii_user_position_guide_t tobii_User_Position, IntPtr user_data)
        {
            gazeData.leftEye.validity = tobii_User_Position.left_position_validity == tobii_validity_t.TOBII_VALIDITY_VALID ? Validity.Valid : Validity.Invalid;
            gazeData.leftEye.origin.x = tobii_User_Position.left_position_normalized_xyz.x;
            gazeData.leftEye.origin.y = tobii_User_Position.left_position_normalized_xyz.y;
            gazeData.leftEye.origin.z = tobii_User_Position.left_position_normalized_xyz.z;

            gazeData.rightEye.validity = tobii_User_Position.right_position_validity == tobii_validity_t.TOBII_VALIDITY_VALID ? Validity.Valid : Validity.Invalid;
            gazeData.rightEye.origin.x = tobii_User_Position.right_position_normalized_xyz.x;
            gazeData.rightEye.origin.y = tobii_User_Position.right_position_normalized_xyz.y;
            gazeData.rightEye.origin.z = tobii_User_Position.right_position_normalized_xyz.z;
        }
        public void EnumerateSupportedStreams()
        {
            /*
               vrg1 t2
               Device supports stream: TOBII_STREAM_DIGITAL_SYNCPORT.
               Device supports stream: TOBII_STREAM_USER_POSITION_GUIDE.
            */

            // Print the name of the device
            string deviceName;
            Interop.tobii_get_device_name(deviceContext, out deviceName);
            Console.WriteLine(deviceName);

            /*
                VR4
                serial_number = VRU02-5A94AAX02421
                model = VR4_U2_P2
                generation = VR4
                firmware_version = 2.41.0-942e3e4
                integration_id =
                hw_calibration_version = 1.100.0-1c2a552
                hw_calibration_date = 2019-05-08 22:08:08.836000
                lot_id =
                integration_type = HMD
                runtime_build_version = 1.16.36.0_a8c7f63
                Device supports stream: TOBII_STREAM_DIGITAL_SYNCPORT.
                Device supports stream: TOBII_STREAM_USER_POSITION_GUIDE.
             */

            // Get all the info about the device
            tobii_device_info_t stats;
            Interop.tobii_get_device_info(deviceContext, out stats);
            Console.WriteLine(stats.generation);

            foreach (var field in typeof(tobii_device_info_t).GetFields(BindingFlags.Instance |
                                                                        BindingFlags.NonPublic |
                                                                        BindingFlags.Public))
            {
                Console.WriteLine("{0} = {1}", field.Name, field.GetValue(stats));
            }

            if (deviceContext == IntPtr.Zero)
            {
                Console.WriteLine("No device was instantiated! Please restart the app.");
                return;
            }

            foreach (tobii_stream_t stream in Enum.GetValues(typeof(tobii_stream_t)))
            {
                bool supports;
                var Error_T = Interop.tobii_stream_supported(deviceContext, stream, out supports);
                if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
                {
                    Console.WriteLine($"An error occured: {Error_T}");
                    continue;
                }
                if (supports)
                {
                    Console.WriteLine($"Device supports stream: {stream}.");
                }
            }
        }
        public bool RegisterCallbacks()
        {
            // Subscribe to raw eye position data
            var Error_T = Interop.tobii_user_position_guide_subscribe(deviceContext, OnPosition);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: Could not subscribe to gaze origin");
                return false;
            }

            Error_T = Interop.tobii_wearable_consumer_data_subscribe(deviceContext, OnWearable);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: Could not subscribe to gaze origin");
                return false;
            }

            return true;
        }

        public void Update()
        {
            tobii_error_t Error_T = tobii_error_t.TOBII_ERROR_NO_ERROR;

            // Block this thread until data is available. Especially useful if running in a separate thread.
            Interop.tobii_wait_for_callbacks(new[] { deviceContext });
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR || !(Error_T == tobii_error_t.TOBII_ERROR_TIMED_OUT)))
            {
                Console.WriteLine("Something bad happened while updating Tobii Eye Callbacks.");
                Console.WriteLine(Error_T);
            }

            // Process callbacks on this thread if data is available
            Interop.tobii_device_process_callbacks(deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while processing Tobii Eye Callbacks.");
                Console.WriteLine(Error_T);
            }
        }
        public void Teardown()
        {
            var Error_T = Interop.tobii_digital_syncport_unsubscribe(deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down gaze point.");
                Console.WriteLine(Error_T);
            }

            Error_T = Interop.tobii_user_position_guide_unsubscribe(deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down gaze origin.");
                Console.WriteLine(Error_T);
            }

            Error_T = Interop.tobii_device_destroy(deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down.");
                Console.WriteLine(Error_T);
            }

            Error_T = Interop.tobii_api_destroy(apiContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down.");
                Console.WriteLine(Error_T);
            }
        }
    }
}
