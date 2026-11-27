using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
namespace ForestCleanup {
public class GameSession : MonoBehaviour {
    public static GameSession Current {get; private set;}
    public PlayerMotor player;
    public Interaction interaction;
    public MonsterBrain monster;
    public int Collected {get; private set;}
    public bool Playing {get; private set;} = true;
    public bool Paused {get; private set;}
    public bool Won {get; private set;}
    public const int Total = 10;
    public Vector3 exit = new Vector3(0,0,-26);
    Font font;
    GUIStyle text, title, button;
    public string notice="쓰레기 10개를 수거한 뒤 입구로 돌아오세요.";
    float noticeUntil=12;
    void Awake() { Current=this; Time.timeScale=1; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},24); }
    public void RecordPickup() {
        Collected++; SoundBank.Play(player.transform.position,700,.2f,.15f);
        if(Collected==1) StartCoroutine(SpawnMonster());
        if(Collected==Total) { notice="모두 수거했습니다. 입구로 돌아가세요!"; noticeUntil=float.PositiveInfinity; }
    }
    IEnumerator SpawnMonster() {
        yield return new WaitForSeconds(10);
        while(Playing) {
            foreach(Vector3 p in new[]{new Vector3(-24,0,24),new Vector3(24,0,24),new Vector3(-24,0,-23),new Vector3(24,0,-23)}) {
                Vector3 screen=player.view.WorldToViewportPoint(p+Vector3.up);
                bool visible=screen.z>0 && screen.x>-.1f && screen.x<1.1f && screen.y>-.1f && screen.y<1.1f && !Physics.Linecast(player.view.transform.position,p+Vector3.up,1<<0);
                if(Vector3.Distance(p,player.transform.position)<18 || visible) continue;
                if(!NavMesh.SamplePosition(p,out var hit,3,NavMesh.AllAreas)) continue;
                monster.transform.position=hit.position; monster.gameObject.SetActive(true);
                notice="숲 어딘가에서 발소리가 들립니다."; noticeUntil=Time.time+5; yield break;
            }
            yield return new WaitForSeconds(.5f);
        }
    }
    void Update() {
        if(Playing && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame) SetPause(!Paused);
        if(!Playing || Paused || player==null) return;
        if(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(exit.x,exit.z))<2.4f) {
            if(Collected==Total) Finish(true);
            else {notice=$"아직 쓰레기 {Total-Collected}개가 남았습니다.";noticeUntil=Time.time+2;}
        }
    }
    public void SetPause(bool value) { Paused=value; Time.timeScale=value?0:1; Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=value; }
    public void Finish(bool won) { if(!Playing)return; Playing=false; Won=won; Time.timeScale=1; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; SoundBank.Play(player.transform.position,won?500:40,.6f,.8f); }
    void OnApplicationFocus(bool focus) { if(!focus && Playing) SetPause(true); }
    public void Restart() { Time.timeScale=1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    void OnGUI() {
        if(text==null) { text=new GUIStyle(GUI.skin.label){font=font,fontSize=22,wordWrap=true}; text.normal.textColor=new Color(.85f,.9f,.82f); title=new GUIStyle(text){fontSize=48}; button=new GUIStyle(GUI.skin.button){font=font,fontSize=24}; }
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f); GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
        float w=Screen.width/scale,h=Screen.height/scale;
        GUI.color=new Color(0,0,0,.65f); GUI.DrawTexture(new Rect(20,20,350,110),Texture2D.whiteTexture);GUI.color=Color.white;
        GUI.Label(new Rect(40,30,330,35),"숲속 청소 / NIGHT SHIFT",text);
        GUI.Label(new Rect(40,70,330,40),$"수거 {Collected} / {Total}",text);
        GUI.Label(new Rect(30,h-42,w-60,35),"WASD 이동   ·   마우스 시점   ·   Shift 달리기   ·   E 줍기   ·   Esc 일시정지",text);
        if(Playing && !Paused) {
            if(player != null && player.StaminaBarVisible) DrawStaminaBar(w, h);
            GUI.Label(new Rect(w/2-6,h/2-16,25,30),"·",text);
            if(interaction!=null && interaction.Target!=null) GUI.Label(new Rect(w/2-60,h/2+35,250,35),"E: 줍기",text);
            if(Time.time<noticeUntil) GUI.Label(new Rect(w/2-300,145,600,75),notice,text);
        } else {
            GUI.color=new Color(0,0,0,.88f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(w/2-230,h/2-140,700,70),Paused?"잠시 멈춤":Won?"숲을 벗어났습니다":"붙잡혔습니다",title);
            GUI.Label(new Rect(w/2-230,h/2-55,550,70),Paused?"계속하려면 버튼을 누르세요.":Won?"오늘 밤의 청소는 끝났습니다.":"발소리에 귀를 기울이고, 시야를 끊으세요.",text);
            if(GUI.Button(new Rect(w/2-150,h/2+40,300,55),Paused?"계속하기":"다시 시작",button)) { if(Paused)SetPause(false);else Restart(); }
            if(GUI.Button(new Rect(w/2-150,h/2+110,300,50),"종료",button)) Application.Quit();
        }
    }
    void DrawStaminaBar(float w, float h) {
        float x = w / 2 - 150, y = h - 86;
        GUI.color = new Color(0, 0, 0, .75f);
        GUI.DrawTexture(new Rect(x - 4, y - 4, 308, 24), Texture2D.whiteTexture);
        GUI.color = new Color(.2f, .25f, .23f);
        GUI.DrawTexture(new Rect(x, y, 300, 16), Texture2D.whiteTexture);
        GUI.color = Color.Lerp(new Color(.95f, .4f, .25f), new Color(.55f, .85f, .7f), player.Stamina);
        GUI.DrawTexture(new Rect(x, y, 300 * player.Stamina, 16), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
}
