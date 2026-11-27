using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace ForestCleanup {
public class ForestSmokeTest : MonoBehaviour {
    static int phase;
    void Check(bool condition,string message) {if(!condition)throw new System.Exception(message);Debug.Log("FOREST CHECK: "+message);}
    IEnumerator Start() {
        yield return null;
        var routine=Run();
        while(true) {
            bool next; object current=null;
            try {next=routine.MoveNext();if(next)current=routine.Current;}
            catch(System.Exception e) {Debug.LogException(e);Application.Quit(1);yield break;}
            if(!next)yield break;
            yield return current;
        }
    }
    void Capture(Camera view) {
        var target=new RenderTexture(1280,720,24);var previous=RenderTexture.active;
        view.targetTexture=target;view.Render();RenderTexture.active=target;
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../ForestPreview.png"),pixels.EncodeToPNG());
        view.targetTexture=null;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(pixels);
    }
    void CheckStamina(GameSession game) {
        var player = game.player;
        player.TickStamina(true, false, 1f);
        Check(player.StaminaBarVisible && player.Stamina == 1f && !player.IsSprinting, "Shift shows stamina without draining while stationary");
        player.TickStamina(true, true, player.sprintDuration / 2);
        Check(Mathf.Abs(player.Stamina - .5f) < .001f && player.IsSprinting, "Running consumes stamina");
        game.SetPause(true);
        player.TickStamina(false, false, 20f);
        Check(Mathf.Abs(player.Stamina - .5f) < .001f, "Pause freezes stamina recovery");
        game.SetPause(false);
        player.TickStamina(false, true, player.staminaRecoveryDelay / 2);
        Check(Mathf.Abs(player.Stamina - .5f) < .001f && !player.IsSprinting, "Walking waits before recovery");
        player.TickStamina(false, true, player.staminaRecoveryDelay / 2 + player.staminaRecoveryDuration / 4);
        Check(Mathf.Abs(player.Stamina - .75f) < .001f && player.StaminaBarVisible, "Walking restores stamina after delay");
        player.TickStamina(true, true, player.sprintDuration);
        Check(player.Stamina == 0 && !player.IsSprinting, "Exhaustion stops sprinting");
        player.TickStamina(true, true, player.staminaRecoveryDelay + player.staminaRecoveryDuration / 2);
        Check(Mathf.Abs(player.Stamina - .5f) < .001f && !player.IsSprinting, "Holding Shift after exhaustion recovers without sprint flicker");
        player.TickStamina(false, true, .01f);
        player.TickStamina(true, true, .1f);
        Check(player.IsSprinting && player.Stamina < .5f, "Releasing and pressing Shift resumes sprinting");
        player.TickStamina(false, false, player.staminaRecoveryDelay + player.staminaRecoveryDuration);
        Check(player.Stamina == 1f && player.StaminaBarVisible, "Full stamina remains visible briefly");
        player.TickStamina(false, false, player.staminaBarHideDelay / 2);
        Check(player.StaminaBarVisible, "Full bar waits before hiding");
        game.SetPause(true);
        player.TickStamina(false, false, 20f);
        Check(player.StaminaBarVisible, "Pause freezes bar hide timer");
        game.SetPause(false);
        player.TickStamina(false, false, player.staminaBarHideDelay);
        Check(!player.StaminaBarVisible, "Full stamina bar hides after delay");
    }
    IEnumerator Run() {
        var g=GameSession.Current;g.SetPause(false);g.player.enabled=false;
        Check(g.Collected==0&&g.Playing&&!g.monster.gameObject.activeSelf,"Fresh session reset");
        Check(g.player.Stamina == 1f && !g.player.StaminaBarVisible && !g.player.IsSprinting, "Fresh session resets stamina and hides bar");
        if(phase==2) {Debug.Log("FOREST_SMOKE_PASS");Application.Quit(0);yield break;}
        if(phase==1) {
            var brain = g.monster;
            var agent = brain.GetComponent<NavMeshAgent>();
            agent.updateRotation = false; // Keep vision direction fixed while testing cone boundaries.
            g.player.Teleport(new Vector3(0,.1f,-12));
            brain.transform.position=new Vector3(0,0,-18);
            brain.transform.rotation=Quaternion.identity;
            brain.gameObject.SetActive(true);
            yield return new WaitForSeconds(.1f);
            Check(brain.SeesPlayer() && brain.State==MonsterBrain.Mode.Chase,"Front-facing visible player triggers chase");

            g.player.Teleport(new Vector3(0,.1f,-24));
            yield return new WaitForSeconds(.1f);
            Check(!brain.SeesPlayer() && brain.State==MonsterBrain.Mode.Wander,"Player behind monster immediately ends chase");
            Check(agent.hasPath && agent.pathStatus==NavMeshPathStatus.PathComplete,"Wandering chooses a reachable random destination");
            Check(Vector3.Distance(agent.destination, new Vector3(0,0,-12)) > .01f,"Wandering discards last seen player position");

            agent.Warp(new Vector3(0,0,-18));
            brain.transform.rotation=Quaternion.identity;
            g.player.Teleport(new Vector3(0,.1f,-12));
            yield return new WaitForSeconds(.1f);
            Check(brain.State==MonsterBrain.Mode.Chase,"Rediscovered player resumes chase");
            g.player.Teleport(new Vector3(0,.1f,-2));
            yield return new WaitForSeconds(.1f);
            Check(!brain.SeesPlayer() && brain.State==MonsterBrain.Mode.Wander,"Player beyond sight range immediately ends chase");

            agent.Warp(new Vector3(0,0,6));
            brain.transform.rotation=Quaternion.Euler(0,180,0);
            g.player.Teleport(new Vector3(0,.1f,0));
            yield return new WaitForSeconds(.1f);
            Check(!brain.SeesPlayer() && brain.State==MonsterBrain.Mode.Wander,"Cabin wall hides player inside vision cone");

            g.player.Teleport(new Vector3(27,.1f,27));
            agent.updateRotation=true;
            Vector3 before=brain.transform.position;
            yield return new WaitForSeconds(1.5f);
            Check(brain.State==MonsterBrain.Mode.Wander && Vector3.Distance(before,brain.transform.position)>.3f,"Monster moves while player is unseen");
            Vector3 destination=agent.destination;
            Check(agent.Warp(destination),"Random destination lies on NavMesh");
            agent.ResetPath();
            yield return new WaitForSeconds(1.1f);
            Check(agent.hasPath && Vector3.Distance(destination,agent.destination)>2f,"Wandering selects another destination after arrival");

            g.player.Teleport(new Vector3(0,.1f,-12));
            agent.Warp(new Vector3(0,0,-12.8f));
            yield return new WaitForSeconds(.2f);
            Check(!g.Playing&&!g.Won,"Contact triggers failure");
            phase=2;g.Restart();yield break;
        }
        CheckStamina(g);
        var items=Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None);
        Check(items.Length==10,"Exactly ten pickups");
        var route=new NavMeshPath();
        Check(NavMesh.SamplePosition(Vector3.zero,out var origin,2,NavMesh.AllAreas),"Cabin is walkable");
        foreach(var item in items) {
            Check(NavMesh.SamplePosition(item.transform.position,out var target,2,NavMesh.AllAreas),"Pickup on navigable ground");
            Check(NavMesh.CalculatePath(origin.position,target.position,NavMesh.AllAreas,route)&&route.status==NavMeshPathStatus.PathComplete,"Pickup reachable from cabin: "+item.name+" at "+target.position);
        }
        g.monster.transform.position=new Vector3(0,0,6);
        Check(!g.monster.ClearSight(new Vector3(0,1.6f,0)),"Cabin wall blocks sight and capture");
        g.monster.transform.position=new Vector3(0,0,-6);
        Check(g.monster.ClearSight(new Vector3(0,1.6f,0)),"Cabin doorway allows sight");
        g.player.Teleport(g.exit+Vector3.up*.1f);yield return null;
        Check(g.Playing,"Exit remains locked before collection");
        g.player.Teleport(new Vector3(0,.1f,-8));g.player.transform.rotation=Quaternion.Euler(0,0,0);
        if(!Application.isBatchMode) {yield return new WaitForSeconds(1); Capture(g.player.view);}
        items[0].Collect();items[0].Collect();Check(g.Collected==1,"Duplicate collection ignored");
        Check(!g.monster.gameObject.activeSelf,"Spawn is delayed");
        yield return new WaitForSeconds(11);
        Check(g.monster.gameObject.activeSelf,"Monster spawns after delay");g.monster.gameObject.SetActive(false);
        for(int i=1;i<items.Length;i++)items[i].Collect();Check(g.Collected==10,"All pickups counted");
        g.player.Teleport(g.exit+Vector3.up*.1f);yield return null;yield return null;
        Check(g.Won&&!g.Playing,"Collected trash plus exit triggers victory");
        g.Finish(false);Check(g.Won,"Terminal state cannot change");
        phase=1;g.Restart();
    }
}
}




