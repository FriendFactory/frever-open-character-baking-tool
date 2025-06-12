using UnityEngine;
using System.Collections;
using System.Text;

public class DemoCharacterDetails : MonoBehaviour
{

	public void CharacterUpdated(UMA.UMAData umaData)
	{
		var sb = new StringBuilder();
		sb.AppendFormat("Vertices {0} Bones {1} Quality {2}\n", umaData.GetRenderer(0).sharedMesh.vertexCount, umaData.GetRenderer(0).bones.Length, umaData.GetRenderer(0).quality);
		
		foreach (var mat in umaData.GetRenderer(0).sharedMaterials)
		{
			sb.AppendFormat("{0} {1}x{2}\n", mat.name, mat.mainTexture.width, mat.mainTexture.height);
		}
		GetComponent<UnityEngine.UI.Text>().text = sb.ToString();
	}
}
