using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace ForestCleanup {
public class ForestWorld : MonoBehaviour {
    public Material baseMaterial;
    public GameObject monsterVisualPrefab;
    Material ground,wood,leaf,stone,path,trash,metal;
    Transform geometry;
    readonly System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();
    Mesh canopyMesh;
    void OnDestroy() { foreach(var material in materials) Destroy(material); if(canopyMesh!=null) Destroy(canopyMesh); }
    public static readonly Vector3[] TrashPositions={new Vector3(-5,.3f,-5),new Vector3(5,.3f,2),new Vector3(-18,.3f,10),new Vector3(-21,.3f,13),new Vector3(-16,.3f,14),new Vector3(18,.3f,13),new Vector3(21,.3f,10),new Vector3(17,.3f,8),new Vector3(-6,.3f,-21),new Vector3(7,.3f,-22)};
    Material Mat(string name,Color c) { var m=new Material(baseMaterial);m.name=name;m.color=c;materials.Add(m);return m; }
    GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 size,Material m,bool solid=true) {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(geometry);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;
        if(!solid) {go.layer=2;go.GetComponent<Collider>().enabled=false;}return go;
    }
    void Awake() {
        var game=gameObject.AddComponent<GameSession>();
        geometry=new GameObject("Forest - navigation geometry").transform;
        ground=Mat("Forest floor",new Color(.12f,.16f,.13f)); wood=Mat("Weathered cedar",new Color(.24f,.19f,.14f));leaf=Mat("Pine needles",new Color(.12f,.23f,.19f));stone=Mat("Cold stone",new Color(.28f,.32f,.31f));path=Mat("Dirt path",new Color(.25f,.24f,.18f));trash=Mat("Discarded cardboard",new Color(.64f,.52f,.32f));metal=Mat("Cans",new Color(.55f,.62f,.62f));
        Shape("Ground",PrimitiveType.Cube,new Vector3(0,-.3f,0),new Vector3(62,.6f,62),ground);
        for(int i=0;i<4;i++) { bool side=i<2; Shape("Forest boundary",PrimitiveType.Cube,new Vector3(side?(i==0?-30:30):0,2,side?0:(i==2?-30:30)),new Vector3(side?1:60,4,side?60:1),stone); }
        // Paths and foliage never participate in navigation or interaction raycasts.
        Shape("Entrance trail",PrimitiveType.Cube,new Vector3(0,.015f,-17),new Vector3(4,.025f,25),path,false);
        for(int i=0;i<48;i++) {float a=i*Mathf.PI*2/48; Shape("Loop trail",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*18,.02f,Mathf.Cos(a)*18),new Vector3(4,.025f,4),path,false);}
        Cabin();
        var rng=new System.Random(42);
        for(int i=0;i<165;i++) {
            float x=(float)rng.NextDouble()*56-28,z=(float)rng.NextDouble()*56-28; Vector3 p=new Vector3(x,0,z);
            if(Mathf.Abs(x)<7 && Mathf.Abs(z)<9 || Mathf.Abs(x)<3 && z<0 || Mathf.Abs(p.magnitude-18)<2.8f)continue;
            bool close=false;foreach(var t in TrashPositions)if(Vector3.Distance(p,t)<2.7f)close=true;if(close)continue;
            float height=4+(float)rng.NextDouble()*4;
            Shape("Pine trunk",PrimitiveType.Cylinder,p+Vector3.up*height/2,new Vector3(.5f,height/2,.5f),wood);
            for(int n=0;n<3;n++) {var crown=Shape("Low poly canopy",PrimitiveType.Cylinder,p+Vector3.up*(height*.55f+n*.95f),new Vector3(3.6f-n*.7f,.8f,3.6f-n*.7f),leaf,false); crown.GetComponent<MeshFilter>().sharedMesh=Cone();}
        }
        for(int i=0;i<8;i++) {float a=i*.8f;Vector3 rockPosition=new Vector3(21+Mathf.Sin(a)*4,.7f,14+Mathf.Cos(a)*3);bool overlaps=false;foreach(var t in TrashPositions)if(Vector3.Distance(rockPosition,t)<2.5f)overlaps=true;if(!overlaps)Shape("Boulder",PrimitiveType.Sphere,rockPosition,new Vector3(2.3f,2,2),stone);}
        Shape("Camp table",PrimitiveType.Cube,new Vector3(-20,.7f,11),new Vector3(3,.3f,1.4f),wood);
        Shape("Camp crate",PrimitiveType.Cube,new Vector3(-18,.45f,13),Vector3.one*.9f,wood);
        for(int i=0;i<2;i++) Shape("Gate post",PrimitiveType.Cube,new Vector3(i==0?-2.6f:2.6f,1.2f,-27),new Vector3(.4f,2.4f,.4f),wood);
        Shape("Exit barrier",PrimitiveType.Cube,new Vector3(0,1.15f,-27),new Vector3(5,.22f,.25f),metal);
        Lamp(new Vector3(0,2.6f,-26),new Color(.55f,.85f,.7f),4,10);
        Sign("EXIT / 출구",new Vector3(0,2.5f,-27),180);
        var surface=geometry.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=1;surface.BuildNavMesh();
        for(int i=0;i<TrashPositions.Length;i++) {var item=Shape("Trash "+(i+1),i%3==0?PrimitiveType.Cylinder:i%3==1?PrimitiveType.Cube:PrimitiveType.Sphere,TrashPositions[i],i%3==0?new Vector3(.3f,.25f,.3f):new Vector3(.55f,.55f,.55f),i%3==0?metal:trash); item.AddComponent<Pickup>();}
        var player=new GameObject("Player");player.layer=2;player.transform.position=new Vector3(0,.1f,0);
        var cc=player.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.28f;cc.center=Vector3.up*.9f;
        game.player=player.AddComponent<PlayerMotor>();
        var camera=new GameObject("Player camera");camera.tag="MainCamera";camera.transform.SetParent(player.transform);camera.transform.localPosition=new Vector3(0,1.6f,0);camera.transform.localRotation=Quaternion.Euler(0,180,0);player.transform.rotation=Quaternion.Euler(0,180,0);camera.transform.localRotation=Quaternion.identity;
        var view=camera.AddComponent<Camera>();view.nearClipPlane=.05f;view.farClipPlane=90;view.fieldOfView=75;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.035f,.055f,.06f);camera.AddComponent<AudioListener>();game.player.view=view;
        var flash=camera.AddComponent<Light>();flash.type=LightType.Spot;flash.range=24;flash.spotAngle=65;flash.innerSpotAngle=35;flash.intensity=7;flash.color=new Color(.85f,.91f,1);flash.shadows=LightShadows.Soft;
        game.interaction=player.AddComponent<Interaction>();game.interaction.view=view;
        var enemy=new GameObject("Monster - replace Visual child only");enemy.SetActive(false);enemy.layer=2;
        var agent=enemy.AddComponent<NavMeshAgent>();agent.radius=.38f;agent.height=2;agent.speed=2;agent.angularSpeed=240;agent.acceleration=14;agent.stoppingDistance=.3f;
        game.monster=enemy.AddComponent<MonsterBrain>();
        var visual=new GameObject("Visual");visual.transform.SetParent(enemy.transform,false);game.monster.visualRoot=visual.transform;
        if(monsterVisualPrefab!=null) Instantiate(monsterVisualPrefab,visual.transform);
        else {var body=Shape("Placeholder body",PrimitiveType.Capsule,Vector3.up*1.1f,new Vector3(.8f,1.1f,.8f),Mat("Monster",new Color(.09f,.07f,.08f)),false);body.transform.SetParent(visual.transform,false);}
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.025f;RenderSettings.fogColor=view.backgroundColor;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.24f,.3f,.34f);
        var moon=new GameObject("Moon").AddComponent<Light>();moon.type=LightType.Directional;moon.transform.rotation=Quaternion.Euler(48,-30,0);moon.color=new Color(.55f,.68f,.85f);moon.intensity=.65f;moon.shadows=LightShadows.Soft;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-forestSmoke")>=0) gameObject.AddComponent<ForestSmokeTest>();
    }
    void Cabin() {
        Shape("Cabin back",PrimitiveType.Cube,new Vector3(0,1.6f,4),new Vector3(8,3.2f,.3f),wood);
        for(int i=0;i<2;i++) {float x=i==0?-4:4;Shape("Cabin side",PrimitiveType.Cube,new Vector3(x,1.6f,0),new Vector3(.3f,3.2f,8),wood);Shape("Wide doorway",PrimitiveType.Cube,new Vector3(i==0?-2.8f:2.8f,1.6f,-4),new Vector3(2.4f,3.2f,.3f),wood);}
        Shape("Cabin roof",PrimitiveType.Cube,new Vector3(0,3.35f,0),new Vector3(9,.3f,9),wood);
        Shape("Cabin table",PrimitiveType.Cube,new Vector3(2.8f,.7f,2.5f),new Vector3(1.5f,1.4f,1),wood);
        Lamp(new Vector3(0,2.7f,-2),new Color(1,.55f,.25f),3,12);
    }
    void Lamp(Vector3 p,Color c,float intensity,float range) {var l=new GameObject("Landmark light").AddComponent<Light>();l.transform.position=p;l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;}
    void Sign(string label,Vector3 p,float rotation) {var go=new GameObject(label);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,rotation,0);var t=go.AddComponent<TextMesh>();t.text="EXIT";t.anchor=TextAnchor.MiddleCenter;t.characterSize=.3f;t.fontSize=50;t.color=new Color(.7f,1,.8f);}
    Mesh Cone() {
        if(canopyMesh!=null)return canopyMesh; const int n=7;var v=new Vector3[n*3];var tri=new int[n*3];for(int i=0;i<n;i++) {float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;v[i*3]=new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f);v[i*3+1]=Vector3.up*.8f;v[i*3+2]=new Vector3(Mathf.Cos(b)*.5f,-.5f,Mathf.Sin(b)*.5f);tri[i*3]=i*3;tri[i*3+1]=i*3+1;tri[i*3+2]=i*3+2;}var mesh=new Mesh();mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();canopyMesh=mesh;return mesh;
    }
}
}



