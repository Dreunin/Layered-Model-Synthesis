using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class ModelSynthesisTests
{
    const string END_TRANSMISSION_PATH = "Assets/Tilesets/EndTransmission.asset";
    
    [Test]
    public void EndTransmissionSynthesis()
    {
        int width = 10;
        int length = 10;
        int height = 5;
        int seed = 1234;
        
        Tileset tileset = AssetDatabase.LoadAssetAtPath<Tileset>(END_TRANSMISSION_PATH);

        ModelSynthesis synthesis = new ModelSynthesis(tileset, width, length, height, seed);
        synthesis.Synthesise();
        Assert.IsTrue(synthesis.tilesPropagated > 0);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator ModelSynthesisWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
    
    
}
