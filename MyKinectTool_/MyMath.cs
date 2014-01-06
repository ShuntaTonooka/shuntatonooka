using Microsoft.Kinect;

namespace MyKinectTool_
{
    class MyMath
    {
        public static float Dot(JointType root, JointType tar, Skeleton user, Vector4[] file)
        {
            Vector4 vec1, vec2;
            vec1 = new Vector4();
            vec2 = new Vector4();

            vec1.X = user.Joints[root].Position.X - user.Joints[tar].Position.X;
            vec1.Y = user.Joints[root].Position.Y - user.Joints[tar].Position.Y;
            vec1.Z = user.Joints[root].Position.Z - user.Joints[tar].Position.Z;

            
            vec2.X = file[(int)root].X - file[(int)tar].X;
            vec2.Y = file[(int)root].Y - file[(int)tar].Y;
            vec2.Z = file[(int)root].Z - file[(int)tar].Z;

            float AA, BB, AB;

            AA = vec1.X * vec1.X + vec1.Y * vec1.Y + vec1.Z * vec1.Z;
            BB = vec2.X * vec2.X + vec2.Y * vec2.Y + vec2.Z * vec2.Z;
            AB = vec1.X * vec2.X + vec1.Y * vec2.Y + vec1.Z * vec2.Z;

            return (float)(AB / (System.Math.Sqrt(AA) * System.Math.Sqrt(BB)));
        }
    }
}
