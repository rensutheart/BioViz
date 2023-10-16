using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadOnClickAsync : MonoBehaviour 
{
	public Slider loadingBar;
	public GameObject loadingImage;

	private AsyncOperation async;


	public void ClickAsync(int scene)
	{
		loadingImage.SetActive(true);
		StartCoroutine(LoadSceneWithBar(scene));
	}
	
	IEnumerator LoadSceneWithBar(int scene)
	{
		async = SceneManager.LoadSceneAsync(scene);

		while(!async.isDone)
		{
			loadingBar.value = async.progress;
			yield return null;
		}
	}
}
