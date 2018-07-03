using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World.Generation.Noise
{
    class OpenSimplex
    {
            private OpenSimplexNoise[] octaves;
        private double[] frequencies, amplitudes;

        private int largestFeature;
        private double pesistence;
        private int seed;
        private double amplitude;
        private double frequency;

        public OpenSimplex(int largestFeature, double persistence, double amp, double freq, int seed) {
            this.largestFeature = largestFeature;
            this.pesistence = persistence;
            this.seed = seed;
            amplitude = amp;
            frequency = freq;

            populate();
        }

        public void populate() {
            int numberOfOctaves = (int) Math.Ceiling(Math.Log10(largestFeature) / Math.Log10(2));

            octaves = new OpenSimplexNoise[numberOfOctaves];
            frequencies = new double[numberOfOctaves];
            amplitudes = new double[numberOfOctaves];

            Random rnd = new Random(seed);

            for (int i = 0; i < numberOfOctaves; i++) {
                octaves[i] = new OpenSimplexNoise(rnd.Next());

                frequencies[i] = Math.Pow(2, i);
                amplitudes[i] = Math.Pow(pesistence, octaves.Length - i);
                amplitudes[i] = amplitude;
                frequencies[i] = frequency;
            }
        }

        public double noise(double x, double y) {
            double result = 0;

            for (int i = 0; i < octaves.Length; i++) {
                //double frequency = Math.pow(2,i);
                //double amplitude = Math.pow(persistence,octaves.length-i);

                result = result + octaves[i].Evaluate(x / frequencies[i], y / frequencies[i]) * amplitudes[i];
            }

            return Math.Round(result);
        }

        public double noise(double x, double y, double z) {
            double result = 0;

            for (int i = 0; i < octaves.Length; i++) {
                //double frequency = Math.pow(2,i);
                //double amplitude = Math.pow(persistence,octaves.length-i);

                result = result + octaves[i].Evaluate(x / frequencies[i], y / frequencies[i], z / frequencies[i]) * amplitudes[i];
            }

            return Math.Round(result);
        }
    }
}
