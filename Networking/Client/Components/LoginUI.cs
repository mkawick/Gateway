using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [SerializeField]
    CanvasGroup canvasGroup = null;

    [SerializeField]
    InputField usernameField = null;
    [SerializeField]
    InputField passwordField = null;

    [SerializeField]
    Text loginFailedMsg = null;

    private GameClient gameClientComponent;
    private Coroutine fadeIn;

    void Start()
    {
        // Hide the error text
        HideText(loginFailedMsg);
        // Hide the login ui until we're connected
        canvasGroup.alpha = 0;

        gameClientComponent = GameClient.Instance;
        gameClientComponent.Client.OnConnect += GameClientComponent_OnConnect;
        if (gameClientComponent.Client.IsConnected())
        {
            GameClientComponent_OnConnect();
        }
        gameClientComponent.Client.OnLoginResponse += ClientInterface_OnLoginResponse;
        if (gameClientComponent.IsLoggedIn)
        {
            ClientInterface_OnLoginResponse(new LoginResponse(true));
        }
    }

    private void HideText(Text text)
    {
        var fadedOut = Color.red;
        fadedOut.a = 0;
        text.color = fadedOut;
    }

    private void GameClientComponent_OnConnect()
    {
        fadeIn = StartCoroutine(FadeInCanvasGroupRoutine());
        // Disable the WindowCanvas's ClickToGame, which will swallow our clicks otherwise
        UIStateController.Instance.gameObject.SetActive(false);
        usernameField.Select();
    }

    public void LoginPressed()
    {
        if (gameClientComponent && gameClientComponent.IsLoggedIn == false)
        {
            var username = usernameField.text;
            var password = passwordField.text;

            gameClientComponent.Client.SendLogin(username, password);
        }
    }

    private void ClientInterface_OnLoginResponse(LoginResponse response)
    {
        if (response.Success == true)
        {
            LoginSuccess();
        }
        else
        {
            LoginFailed();
        }
    }

    void LoginSuccess()
    {
        if (fadeIn != null)
        {
            StopCoroutine(fadeIn);
            fadeIn = null;
        }
        StartCoroutine(FadeOutCanvasGroupRoutine());
        // Re-enable the ClickToGame layer
        UIStateController.Instance.gameObject.SetActive(true);
    }

    void LoginFailed()
    {
        passwordField.text = "";
        StartCoroutine(TweenFailedMsgRoutine());
    }

    IEnumerator TweenFailedMsgRoutine()
    {
        loginFailedMsg.color = Color.red;
        yield return new WaitForSeconds(1.0f);
        var tweenColor = Color.red;
        tweenColor.a = 0;
        loginFailedMsg.CrossFadeColor(tweenColor, 1.0f, true, true);
    }

    IEnumerator FadeOutCanvasGroupRoutine()
    {
        float time = 1f;
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / time;
            yield return null;
        }
    }

    IEnumerator FadeInCanvasGroupRoutine()
    {
        float time = 1f;
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime / time;
            yield return null;
        }
        fadeIn = null;
    }
}
