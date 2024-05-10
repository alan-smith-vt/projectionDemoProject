using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSystemHandler : MonoBehaviour
{
    [Header("UI Menu")]
    public GameObject Interaction_Menu;

    [Header("Sound")]
    public AudioSource sound_Start_Finish;
    public AudioSource sound_camera_shutter;
    public AudioSource sound_Photo;
    public GameObject UILoading;

    private CameraManager cameraManager;

    public enum UIStates
    {
        none = 0,
        Interaction_Menu = 1,
    }
    private readonly Dictionary<UIStates, List<GameObject>> stateObjects = new Dictionary<UIStates, List<GameObject>>
    {
        {UIStates.Interaction_Menu, new List<GameObject>{} },
    };

    private void Awake()
    {
        cameraManager = GameObject.Find("MixedRealityPlayspace/Camera Manager").GetComponent<CameraManager>();
        Scene scene = SceneManager.GetActiveScene();

        //UIStates from Interaction_Menu
        this.transform.Find("Interaction_Menu/Button_Collection/Take_Picture_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { cameraManager.TakeDelayedPhoto(); StartCoroutine(SoundWaitPhoto()); });

        GameObject debugObjects = GameObject.Find("DebugObjects");
        if (!Application.isEditor)
        {
            debugObjects.SetActive(false);
        }
    }

    void Start()
    {
        //Initilize UI element dict
        stateObjects[UIStates.Interaction_Menu].Add(Interaction_Menu);

        ChangeUIState(UIStates.Interaction_Menu);
    }
    void Update()
    {
        UILoading.transform.Find("Canvas/Image").Rotate(0, 0, 50f * Time.deltaTime);
    }
    public void ChangeUIState(UIStates state)
    {
        ClearAllUI();
        List<GameObject> TempList = new List<GameObject>();//Create a List to hold return of dict call
        TempList = stateObjects[state]; //assign list values from the reference passed UI state into temp list
        foreach (GameObject listgameobject in TempList)
        {
            listgameobject.SetActive(true);//set the specific gameobjects active
        }

    }
    private void ClearAllUI()
    {
        foreach (KeyValuePair<UIStates, List<GameObject>> entry in stateObjects)
        {
            foreach (GameObject go in entry.Value)
            {
                go.SetActive(false);
            }
        }
    }

    IEnumerator SoundWaitPhoto()
    {
        UILoading.transform.Find("PictureFrame").gameObject.SetActive(true);
        sound_Photo.Play();
        yield return new WaitForSeconds(3);
        UILoading.transform.Find("PictureFrame").gameObject.SetActive(false);
        sound_Photo.Stop();
        sound_camera_shutter.Play();
        UILoading.transform.Find("Canvas").gameObject.SetActive(true);
    }
}
