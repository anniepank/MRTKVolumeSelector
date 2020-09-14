using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
// using UnityEngine;

namespace UnityTools
{
    public class CTPositioning
    {
        private const string anacondaActivateBatFilepath = @"C:\ProgramData\Anaconda3\Scripts\activate.bat";
        private const string pythonPath = "\"C:\\ProgramData\\Anaconda3\\python.exe\" \"";
        private static string folder;
        private static float ctScaleFactor = 0.012f;

        private static Dictionary<string, Vector3> ParseCoordinatesFromFile(string filepath)
        {
            var positions = new Dictionary<string, Vector3>();
            var lines = new List<string>(File.ReadLines(filepath));
            for (var i = 0; i < lines.Count; i++)
            {
                if (!lines[i].Contains("#"))
                {
                    var split = lines[i].Split(' ');
                    var coordinates = new Vector3(
                        float.Parse(split[1]),
                        float.Parse(split[2]),
                        float.Parse(split[3])
                        );
                    positions.Add(split[0], coordinates);
                }
            }
            return positions;
        }

        private static float GetScaleFactor(Dictionary<string, Vector3> realCoordinates, Dictionary<string, Vector3> flowCoordinates)
        {
            // not all of the real coordinates might be in the export file from 3dflow,
            // while it sometimes uses less frames than were given in the input
            var numberOfDistances = 0;
            var averageScale = 0f;
            foreach (var flowCoordinate1 in flowCoordinates)
            {
                var frame1 = flowCoordinate1.Key;
                foreach (var flowCoordinate2 in flowCoordinates)
                {
                    var frame2 = flowCoordinate2.Key;

                    if (frame1 != frame2 && realCoordinates.ContainsKey(frame1) && flowCoordinates.ContainsKey(frame2))
                    {
                        var distanceReal = Vector3.Distance(realCoordinates[frame1], realCoordinates[frame2]);
                        var distanceFlow = Vector3.Distance(flowCoordinates[frame1], flowCoordinates[frame2]);

                        averageScale += distanceReal / distanceFlow;
                        numberOfDistances += 1;
                    }
                }
            }

            averageScale /= numberOfDistances;
            return averageScale;
        }

        private static float ConvertToRadians(float angle)
        {
            return (float)(Math.PI / 180) * angle;
        }

        private static float ConvertToDegrees(float rad)
        {
            return (float)(180 / Math.PI) * rad;
        }

        private static Quaternion rotate3dflowToWorld(float rotation_x_3dflow, float rotation_y_3dflow, float rotation_z_3dflow)
        {
            var x_rot = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), ConvertToRadians(-rotation_x_3dflow));
            var y_rot = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), ConvertToRadians(-rotation_z_3dflow));
            var z_rot = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), ConvertToRadians(-rotation_y_3dflow));
            var rotation = x_rot * z_rot * y_rot;
            rotation = rotation * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), ConvertToRadians(90));
            return rotation;

        }

        private static Vector3 MatchPosition(Vector3 currentPotision, Vector3 targetPosition, Quaternion difference)
        {
            return targetPosition - Vector3.Transform(currentPotision, difference); // difference * currentPotision;
        }

        private static Quaternion AverageQuaternions(List<Quaternion> multipleRotations)
        {
            //Global variable which holds the amount of rotations which
            //need to be averaged.
            int addAmount = 0;

            //Global variable which represents the additive quaternion
            Quaternion addedRotation = Quaternion.Identity;

            //The averaged rotational value
            Quaternion averageRotation = new Quaternion();

            //Loop through all the rotational values.
            foreach (Quaternion singleRotation in multipleRotations)
            {
                //Temporary values
                float w;
                float x;
                float y;
                float z;

                //Amount of separate rotational values so far
                addAmount++;

                float addDet = 1.0f / (float)addAmount;
                addedRotation.W += singleRotation.W;
                w = addedRotation.W * addDet;
                addedRotation.X += singleRotation.X;
                x = addedRotation.X * addDet;
                addedRotation.Y += singleRotation.Y;
                y = addedRotation.Y * addDet;
                addedRotation.Z += singleRotation.Z;
                z = addedRotation.Z * addDet;

                //Normalize. Note: experiment to see whether you
                //can skip this step.
                float D = 1.0f / (w * w + x * x + y * y + z * z);
                w *= D;
                x *= D;
                y *= D;
                z *= D;

                //The result is valid right away, without
                //first going through the entire array.
                averageRotation = new Quaternion(x, y, z, w);
            }

            return averageRotation;
        }

        private static Vector3 ToEulerAngles(Quaternion q)
        {
            var angles = new Vector3();

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = ConvertToDegrees((float) Math.Atan2(sinr_cosp, cosr_cosp));

            // pitch (y-axis rotation)
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                angles.Y = ConvertToDegrees((float) (Math.Sign(sinp) * Math.Abs(Math.PI / 2))); // use 90 degrees if out of range
            else
                angles.Y = ConvertToDegrees((float) Math.Asin(sinp));

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = ConvertToDegrees((float)Math.Atan2(siny_cosp, cosy_cosp));

            return angles;
        }

        private static Dictionary<string, List<float>> Calculate3DFlowWorldTransform(float scaleFactor)
        {
            var positions = new List<Vector3>();
            var quaternions = new List<Quaternion>();
            var worldsRotationDifference = new Quaternion();

            string[] lines3DFlowPositionsFile = File.ReadAllLines(folder + @"\export\externals.txt", Encoding.UTF8);
            string[] linesRealPositionsFile = File.ReadAllLines(folder + @"\externals.txt", Encoding.UTF8);
            for (var i = 0; i < lines3DFlowPositionsFile.Length; i++)
            {
                // REAL
                var split = linesRealPositionsFile[i].Split(' ');
                var x = float.Parse(split[1]);
                var y = float.Parse(split[2]);
                var z = float.Parse(split[3]);

                var rotationXRealCamera = float.Parse(split[4]);
                var rotationYRealCamera = float.Parse(split[5]);
                var rotationZRealCamera = float.Parse(split[6]);

                var realCameraPosition = new Vector3(x, y, z);
                var realCameraRotation = Quaternion.CreateFromYawPitchRoll(ConvertToRadians(rotationYRealCamera), ConvertToRadians(rotationXRealCamera), ConvertToRadians(rotationZRealCamera));

                // 3D FLOW
                var line = lines3DFlowPositionsFile[i];
                split = line.Split(' ');
                x = float.Parse(split[1]);
                y = float.Parse(split[2]);
                z = float.Parse(split[3]);

                var rotationX3DFlow = float.Parse(split[4]);
                var rotationY3DFlow = float.Parse(split[5]);
                var rotationZ3DFlow = float.Parse(split[6]);
                
                var cameraPosition3DFlow = new Vector3(x * scaleFactor, z * scaleFactor, y * scaleFactor);
                var newRotation = rotate3dflowToWorld(rotationX3DFlow, rotationY3DFlow, rotationZ3DFlow);

                var quaternion3DFlow = rotate3dflowToWorld(rotationX3DFlow, rotationY3DFlow, rotationZ3DFlow);
                var quaternionReal = realCameraRotation * Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), ConvertToRadians(180));
                worldsRotationDifference = quaternionReal * Quaternion.Inverse(quaternion3DFlow);

                quaternions.Add(worldsRotationDifference);
                positions.Add(MatchPosition(cameraPosition3DFlow, realCameraPosition, worldsRotationDifference));
            }

            var res = new Dictionary<string, List<float>>();
            var position = new Vector3(
                positions.Select(v => v.X).ToList().Sum() / positions.Count,
                positions.Select(v => v.Y).ToList().Sum() / positions.Count,
                positions.Select(v => v.Z).ToList().Sum() / positions.Count
                );

            var positionList = new List<float>();
            positionList.Add(position.X);
            positionList.Add(position.Y);
            positionList.Add(position.Z);
            res.Add("delta position", positionList);

            var q = AverageQuaternions(quaternions);
            var rotationList = new List<float>();
            rotationList.Add(q.X);
            rotationList.Add(q.Y);
            rotationList.Add(q.Z);
            rotationList.Add(q.W);
            res.Add("rotation", rotationList);
            return res;
        }


        private static void PrepareCTFileForRegistration(string filepath, float scaleFactor)
        {
            var builder = new StringBuilder();
            var builder2 = new StringBuilder();
            var lines = new List<string>(File.ReadLines(filepath));

            using (var fileObj = new StreamWriter(folder + @"ct\ct_scaled_axes_switched.obj"))
            {
                using (var fileForRegistration = new StreamWriter(folder + @"ct\ct_scaled_axes_switched.xyz"))
                {
                    for (var i = 0; i < lines.Count; i++)
                    {
                        if (lines[i][0] == 'v')
                        {

                            var split = lines[i].Split(' ');
                            var x = float.Parse(split[1]);
                            var y = float.Parse(split[2]);
                            var z = float.Parse(split[3]);

                            builder.AppendFormat("v {0} {1} {2}\n", -x * scaleFactor, z * scaleFactor, y * scaleFactor);
                            builder2.AppendFormat("{0} {1} {2}\n", -x * scaleFactor, z * scaleFactor, y * scaleFactor);
                        }
                        else
                        {
                            builder.AppendLine(lines[i]);
                        }
                    }
                    fileForRegistration.Write(builder2.ToString());
                    fileObj.Write(builder.ToString());
                }
            }
        }

        private static void Prepare3DFlowModelForRegistration(string filepath, float scaleFactor)
        {
            var builder = new StringBuilder();
            var lines = new List<string>(File.ReadLines(filepath));

            using (var file = new StreamWriter(folder + @"export\3dflow_scaled_axes_switched.xyz"))
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    if (lines[i][0] == 'v')
                    {

                        var split = lines[i].Split(' ');
                        var coordinates = new Vector3(
                            float.Parse(split[1]),
                            float.Parse(split[2]),
                            float.Parse(split[3])
                            );

                        file.WriteLine((-coordinates.X * scaleFactor).ToString("0.00000") + " "
                        + (coordinates.Z * scaleFactor).ToString("0.00000") + " "
                        + (coordinates.Y * scaleFactor).ToString("0.00000"));
                    }
                }
            }


        }

        private static void ApplyTransformObjFile(string filepath, string transformationMatrixFile)
        {
            var builder = new StringBuilder();
            var matrixFileLines = new List<string>(File.ReadLines(transformationMatrixFile));
            var objLines = new List<string>(File.ReadLines(filepath));
            var r0 = matrixFileLines[0].Split(' ');
            var r1 = matrixFileLines[1].Split(' ');
            var r2 = matrixFileLines[2].Split(' ');
            var r3 = matrixFileLines[3].Split(' ');
            var transformationMatrix = new Matrix4x4();
            transformationMatrix.M11 = float.Parse(r0[0]);
            transformationMatrix.M12 = float.Parse(r0[1]);
            transformationMatrix.M13 = float.Parse(r0[2]);
            transformationMatrix.M14 = float.Parse(r0[3]);
            transformationMatrix.M21 = float.Parse(r1[0]);
            transformationMatrix.M22 = float.Parse(r1[1]);
            transformationMatrix.M23 = float.Parse(r1[2]);
            transformationMatrix.M24 = float.Parse(r1[3]);
            transformationMatrix.M31 = float.Parse(r2[0]);
            transformationMatrix.M32 = float.Parse(r2[1]);
            transformationMatrix.M33 = float.Parse(r2[2]);
            transformationMatrix.M34 = float.Parse(r2[3]);
            transformationMatrix.M41 = float.Parse(r3[0]);
            transformationMatrix.M42 = float.Parse(r3[1]);
            transformationMatrix.M43 = float.Parse(r3[2]);
            transformationMatrix.M44 = float.Parse(r3[3]);

            transformationMatrix = Matrix4x4.Transpose(transformationMatrix);

            using (var file = new StreamWriter(folder + @"export\transformed_ct.obj"))
            {
                for (var i = 0; i < objLines.Count; i++)
                {
                    if (objLines[i][0] == 'v')
                    {

                        var split = objLines[i].Split(' ');
                        var point = new Vector3(
                            float.Parse(split[1]),
                            float.Parse(split[2]),
                            float.Parse(split[3])
                            );

                        var result = Vector3.Transform(point, transformationMatrix);
                        file.WriteLine("v " + result.X.ToString() + " " + result.Y.ToString() + " " + result.Z.ToString());
                    }
                    else
                    {
                        file.WriteLine(objLines[i]);
                    }
                }
            }

        }

        private static void RunRegistrationProcess()
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            p.Start();
            using (var sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(anacondaActivateBatFilepath);
                    sw.WriteLine(pythonPath + folder + "registration.py\"");
                }
            }
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
        }

       
        public static Dictionary<string, List<float>> PositionCT(string f)
        {
            folder = f;
            var realPositions = ParseCoordinatesFromFile(folder + "externals.txt");
            var flowPositions = ParseCoordinatesFromFile(folder + @"export\externals.txt");

            var scaleFactor3DFlow = GetScaleFactor(realPositions, flowPositions);

            var predefinedCTTransform = Calculate3DFlowWorldTransform(scaleFactor3DFlow);
            /*
            var sw = new Stopwatch();
            sw.Start();
            Prepare3DFlowModelForRegistration(folder + @"export\3dflow.obj", scaleFactor3DFlow); // export\3dflow_scaled_axes_switched.xyz
            sw.Stop();
            Console.WriteLine("3dflow preparation time");
            Console.WriteLine(sw.Elapsed);

            sw.Start();
            PrepareCTFileForRegistration(folder + @"ct\465_model_509E2E540E363C39FCC3B1981E7D1C48_knee_skin.obj", ctScaleFactor);
            // ct\ct_scaled_axes_switched.obj ct\ct_scaled_axes_switched.xyz
            sw.Stop();
            Console.WriteLine("ct preparation time");
            Console.WriteLine(sw.Elapsed);
            */
            /*
            sw.Start();
            RunRegistrationProcess();
            sw.Stop();
            Console.WriteLine("ct registration time");
            Console.WriteLine(sw.Elapsed);
            */
            ApplyTransformObjFile(folder + @"ct\ct_scaled_axes_switched.obj", folder + @"export\transformation_matrix.txt");
            // export\transformed_ct.obj
            Console.WriteLine("End");
            return predefinedCTTransform;
        }
    }
}
