using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Text;

namespace AzureIotEdgeSimulatedWaveSensor
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestSinWave()
        {
            WaveSensor ws1 = new WaveSensor(freq: 0.15915, amp: 1, vert: 0, delta: 1, type: 1);
            WaveSensor ws2 = new WaveSensor(freq: 0.15915, amp: 1, vert: 0, delta: .05, type: 1);
            WaveSensor ws3 = new WaveSensor(freq: 0.5, amp: 6, vert: 4, delta: .2, type: 1);

            double[] readBlock1 = ws1.read();
            double[] readBlock2 = ws2.read();
            double[] readBlock3 = ws3.read();


            StringBuilder sb = new StringBuilder("Data for Sin\n");
            sb.Append("Time between reads (x-axis) is 1 seconds\n");
            for (int i = 0; i < readBlock1.Length; i++)
            {
                sb.AppendFormat("{0},", readBlock1[i]);
            }
            sb.Append("\nTime between reads (x-axis) is 1 seconds\n");
            for (int i = 0; i < readBlock2.Length; i++)
            {
                sb.AppendFormat("{0},", readBlock2[i]);
            }
            sb.Append("\nApply shifts and rescalings\n");
            for (int i = 0; i < readBlock3.Length; i++)
            {
                sb.AppendFormat("{0},", readBlock3[i]);
            }

            System.IO.File.WriteAllText("\\Users\\Archer Morgan\\Documents\\WaveDataSin.csv", sb.ToString());
        }

        [TestMethod]
        public void TestSquareWave()
        {
            WaveSensor ws1 = new WaveSensor(freq: 0.15915, amp: 1, vert: 0, delta: 1, type: 2);
            WaveSensor ws2 = new WaveSensor(freq: 0.15915, amp: 1, vert: 0, delta: .05, type: 2);
            WaveSensor ws3 = new WaveSensor(freq: 0.5, amp: 6, vert: 4, delta: .2, type: 2);

            double[] readBlock1 = ws1.read();
            double[] readBlock2 = ws2.read();
            double[] readBlock3 = ws3.read();


            StringBuilder sb = new StringBuilder("Data for Square\n");
            sb.Append("Time between reads (x-axis) is 1 seconds\n");
            for (int i = 0; i < readBlock1.Length; i++)
            {
                sb.AppendFormat("{0},", readBlock1[i]);
            }
            sb.Append("\nTime between reads (x-axis) is 1 seconds\n");
            for (int i = 0; i < readBlock2.Length; i++)
            {
                sb.AppendFormat("{0},", readBlock2[i]);
            }
            sb.Append("\nApply shifts and rescalings\n");
            for (int i = 0; i < readBlock3.Length; i++)
            {
                sb.AppendFormat("{0},", readBlock3[i]);
            }

            System.IO.File.WriteAllText("\\Users\\Archer Morgan\\Documents\\WaveDataSquare.csv", sb.ToString());

        }
    }
}
