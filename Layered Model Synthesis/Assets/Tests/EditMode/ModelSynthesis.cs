using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ModelSynthesis
{
    // A Test behaves as an ordinary method
    [Test]
    public void ModelSynthesisSimplePasses()
    {
        Possibility p = new Possibility(null, Rotation.zero);
        Assert.IsNotNull(p);
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
