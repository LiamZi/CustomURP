using UnityEngine;

namespace CustomURP
{
    public class VolumeCloudEditorConfig : ScriptableObject
    {
        public CurlNoiseGenerator _testGenerator;
        public Tex3DGenerator _tex3DGenerator;
        public string _tex3DGeneratorSaveName = "Tex3D";
        public Tex3DGeneratorSecond _tex3DGeneratorSecond;
        public string _tex3DSecondSaveName = "Tex3DSecond";
    }
}
