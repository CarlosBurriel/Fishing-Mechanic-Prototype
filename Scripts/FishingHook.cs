using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class FishingHook : MonoBehaviour
{
    #region Public Variables
    public FishingPlayerController playerController;
    public GameObject fishingCanvas;
    public CinemachineBasicMultiChannelPerlin cinemachineCameraShake;

    [Header("Fishing Bar")]
    public Image currentProgressBar;
    public Color fillColor;
    public int maximumProgress; // Cambiar a Variable según el tamaño en Canvas
    [SerializeField]private int currentProgress;
    public GameObject[] limitPointPressets;

    [Header("Click Event")]
    public int maxClicksNeeded;
    public int minClicksNeeded;

    [Header("Hook Interaction")]
    public float impulseForce;
    #endregion

    #region Private / Hidden Variables
    //Logical Bools
    private bool fishThrowed;
    private bool hookStillThrowed;
    private bool fishingStarted;
    private bool canFillBar;
    private bool SpamClickEvent;

    //Clicks Spamed Event
    private int clicksSpamed;
    private int currentLimitPointClicksNeeded;
    private TextMeshProUGUI currentClicksText;

    //Progress Bars
    private GameObject[] pressetProgressBars;
    private GameObject currentPresset;
    private int ProgressBarsAuxIndex;

    //Feedback
    bool ShakeCanvasActive = true;

    //Input System
    public InputSystem_Actions fishingMinigameInputSystem;
    #endregion

    private void Start()
    {
        cinemachineCameraShake = transform.parent.GetChild(0).GetComponent<CinemachineBasicMultiChannelPerlin>();
        transform.GetChild(0).GetComponent<ParticleSystem>().Stop();
        fishingCanvas.SetActive(false);  
        fishingMinigameInputSystem = playerController.playerInputSystem;
        fishingMinigameInputSystem.FishingDemo.ThrowFishingRod.performed += ThrowHook;
    }

    #region Throw & Pick Up Hook
    public void ThrowHook(InputAction.CallbackContext context)
    {
        if (!fishThrowed)
        {
            gameObject.GetComponent<SphereCollider>().enabled = true; playerController.playerCanMove = false;
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
            gameObject.GetComponent<Rigidbody>().AddForce(transform.parent.forward * impulseForce, ForceMode.Impulse);
            fishThrowed = true;
        }
        else
            PickUpHook();
    }
    public void PickUpHook()
    {
        hookStillThrowed = false;
        transform.GetChild(0).GetComponent<ParticleSystem>().Stop(); //Particles
        transform.DOMove(transform.parent.GetChild(1).position, 0.75f).SetEase(Ease.InOutExpo).OnComplete(() => { gameObject.GetComponent<SphereCollider>().enabled = false; playerController.playerCanMove = true; gameObject.GetComponent<Rigidbody>().isKinematic = true; });
        fishThrowed = false;
        fishingMinigameInputSystem.FishingDemo.InteractFishingRod.performed -= InteractWithBar;
        fishingMinigameInputSystem.FishingDemo.InteractFishingRod.canceled -= ReleaseInteractWithBar;
        fishingMinigameInputSystem.FishingDemo.ThrowFishingRod.performed += ThrowHook;
    }
    #endregion

    #region Fishing Minigame Input System
    private void InteractWithBar(InputAction.CallbackContext context)
    {
        if (SpamClickEvent)
        {
            SpamClickAtLimitPoint();
            cinemachineCameraShake.enabled = true;
            return;
        }
        canFillBar= true;
    }
    private void ReleaseInteractWithBar(InputAction.CallbackContext context)
    {
        canFillBar = false;
    }
    #endregion

    #region Fishing Minigame
    private void OnTriggerEnter(Collider other) //Trigger the minigame
    {
        if (other.gameObject.layer == 9)
        {
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
            gameObject.GetComponent<SphereCollider>().enabled = false;
            hookStillThrowed = true;
            StartCoroutine(CaughtFish());
        }
        else if(other.gameObject.tag == "FishingDemoGround") { PickUpHook(); } 
    }
    IEnumerator CaughtFish()
    {
        int RandomTime = Random.Range(1, 15);
        yield return new WaitForSeconds(RandomTime);
        if (hookStillThrowed)
        {
            fishingMinigameInputSystem.FishingDemo.InteractFishingRod.performed += InteractWithBar;
            fishingMinigameInputSystem.FishingDemo.InteractFishingRod.canceled += ReleaseInteractWithBar;
            fishingMinigameInputSystem.FishingDemo.ThrowFishingRod.performed -= ThrowHook;
            //Feedback
            transform.GetChild(0).GetComponent<ParticleSystem>().Play(); //Particles
            cinemachineCameraShake.enabled = true;
            yield return new WaitForSeconds(1f);
            cinemachineCameraShake.enabled = false;

            StartFishingMinigame();
        }
        
    }
    private void StartFishingMinigame()
    {
        fishingCanvas.gameObject.SetActive(true);
        currentProgress = 0;
        currentPresset = limitPointPressets[Random.Range(0, limitPointPressets.Length)];
        currentPresset = Instantiate(currentPresset, fishingCanvas.transform);
        pressetProgressBars = currentPresset.GetComponent<LimitPoints>().SelfLimitPoints;
        currentProgressBar = currentPresset.transform.GetChild(currentPresset.transform.childCount - 1).GetComponent<Image>();
        fishingStarted = true;
    }

    private void Update()
    {
        if (fishingStarted)
        {
            if (canFillBar)
            {
                currentProgress++;
                GetCurrentProgress();
                ShakeCanvas(); //Feedback
            }
        }
    }

    private void GetCurrentProgress()
    {
        float fillAmount = (float)currentProgress / (float)maximumProgress;
        currentProgressBar.fillAmount = fillAmount;
        currentProgressBar.color = fillColor;

        if (pressetProgressBars[pressetProgressBars.Length - 1].GetComponent<Image>().fillAmount >= 1) //Check if the minigame has ended
        {
            fishingStarted = false;
            PickUpHook();
            fishingCanvas.gameObject.SetActive(false);
            ProgressBarsAuxIndex = 0;
            currentProgress = 0;
            currentPresset.SetActive(false); currentPresset = null;
        }
        else if (currentProgressBar.fillAmount >= 0.72f && currentProgressBar.fillAmount <= 0.75f && pressetProgressBars[ProgressBarsAuxIndex] != pressetProgressBars[pressetProgressBars.Length - 1]) //Set the Spam Clicks Event feedback for the player
        {
            currentLimitPointClicksNeeded = Random.Range(minClicksNeeded, maxClicksNeeded + 1);
            currentClicksText = currentProgressBar.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
            currentClicksText.text = currentLimitPointClicksNeeded.ToString();
            currentClicksText.transform.parent.gameObject.SetActive(true);
        }
        else if (currentProgressBar.fillAmount >= 1)
        {
            currentProgress = 0;
            clicksSpamed = 0;
            SpamClickEvent = true;
            canFillBar = false;
            ProgressBarsAuxIndex++;
            currentProgressBar = currentPresset.transform.GetChild((currentPresset.transform.childCount - 1) - ProgressBarsAuxIndex).GetComponent<Image>();
        } 

        
    }
    private void SpamClickAtLimitPoint()
    {
        clicksSpamed++;
        currentClicksText.text = (currentLimitPointClicksNeeded - clicksSpamed).ToString();
        //Feedback
        currentClicksText.transform.parent.DOPunchScale(new Vector3(1.05f, 1.05f, 1.05f), 0.5f, 10, 1)
            .OnComplete(() => { currentClicksText.transform.parent.DOScale(new Vector3(1f, 1f, 1f), 0.5f); cinemachineCameraShake.enabled = false; });

        if (clicksSpamed >= currentLimitPointClicksNeeded)
        {
            currentClicksText.transform.parent.gameObject.SetActive(false);
            SpamClickEvent = false;
        }
    }

    private void ShakeCanvas()
    {
        if (currentPresset == null) { return; }
        if (ShakeCanvasActive) //Assure that the Tween has finished to avoid position recolocations
        {
            ShakeCanvasActive = false;
            Sequence ShakeSequence = DOTween.Sequence();
            ShakeSequence.Join(currentPresset.transform.DOShakePosition(0.1f, 1.5f, 10, 90, false, false)).Join(currentPresset.transform.parent.GetChild(1).transform.DOShakePosition(0.1f, 0.1f, 10, 90, false, false)).OnComplete(() => ShakeCanvasActive = true) ;
        }
       
    }
    #endregion
}
