# 숲속 청소 / Forest Cleanup

Unity 6000.0.50f1 · URP · Windows x64

## 실행
- `Assets/Scenes/ForestCleanup.unity`를 열고 Play.
- 씬이 없다면 메뉴 `Forest Cleanup > Create or refresh game scene` 실행.
- Windows 빌드: `Builds/Windows/ForestCleanup.exe`.
- WASD 이동 / 마우스 시점 / Shift 달리기 / E 줍기 / Esc 일시정지.
- 쓰레기 10개를 수거하고 남쪽의 녹색 EXIT 차단기 앞으로 돌아오면 승리.
- 첫 수집 10초 뒤 괴물이 등장. 오두막도 안전하지 않음.

## 구성과 조정
맵은 ForestWorld.Awake에서 고정 시드로 생성됩니다. 편집 모드에서 비어 보이는 것은 정상입니다.
ForestWorld의 TrashPositions가 10개 수집 위치이며 Cabin, 나무 배치, 조명도 이 파일에서 조정합니다.
PlayerMotor: 걷기 3, 달리기 5, 마우스 감도 0.09.
스태미나: 최대 6초 달리기, 달리기를 멈춘 뒤 2초 후 회복 시작, 빈 상태에서 4초 동안 완전 회복, 완충 후 1.5초 뒤 바 숨김.
Shift를 누르면 화면 하단에 바가 나타나며, 이동하며 달릴 때만 소모됩니다. 완전히 소진되면 걷기로 전환되며, 회복 후 Shift를 놓았다 다시 눌러 달릴 수 있습니다.
스태미나 시간은 PlayerMotor의 Stamina 설정 필드에서 조정할 수 있습니다. 일시정지 중에는 소모·회복·숨김 타이머가 멈춥니다.
MonsterBrain: 전방 12m/120도 시야(표시 없음), 배회 속도 2, 추격 속도 4. 장애물·시야각·거리로 플레이어가 보이지 않으면 즉시 추격을 멈추고 주변 14m 내 도달 가능한 무작위 지점으로 이동합니다. 도착 후 새 지점을 고르고, 다시 발견하면 추격합니다. Inspector에서 sightRange, fieldOfView, wanderRadius를 조정할 수 있습니다.
GameSession: 수집 수, 지연 스폰, 승패, 일시정지, 한국어 HUD.

## 친구의 괴물 모델 적용
1. FBX와 재질을 Assets 아래에 임포트하고 Prefab을 만듭니다.
2. Prefab은 발바닥이 원점, +Z가 정면, 높이 약 2m가 되도록 맞춥니다.
3. ForestCleanup 씬의 Forest Cleanup 오브젝트에서 Monster Visual Prefab에 지정합니다.
4. 모델에는 이동 스크립트/Collider/NavMeshAgent를 넣지 않습니다. Animator의 Apply Root Motion은 끕니다.
5. 외형만 Visual 자식으로 생성되므로 모델 없이도 게임은 동작합니다.
주의: Create or refresh game scene 메뉴는 씬을 재생성하며 모델 연결을 초기화합니다.

## 에셋
현재 배경은 직접 생성한 기본 도형과 7면 나무 메시입니다. 외부 모델은 포함하지 않았습니다.
Kenney Nature Kit 다운로드가 네트워크 오류로 완료되지 않아 로컬 생성 배경으로 대체했습니다.
후보: https://kenney.nl/assets/nature-kit (제작자 페이지에 CC0 명시).
소리는 코드로 합성한 발걸음/수집/종료 효과음이며 외부 음원은 없습니다.

## 빌드 및 자동 검증
Unity 배치 실행의 `-executeMethod ForestBuild.Build`로 씬 생성 및 Windows 개발 빌드.
실행 파일에 `-forestSmoke -batchmode`를 전달하면 자동 검증하고 성공 0/실패 1로 종료합니다.
로그에 `FOREST_SMOKE_PASS`가 있어야 통과입니다. 빌드 로그: Logs/ForestBuild.log.
자동 검증은 수동 플레이의 체감 난이도와 5~8분 플레이 시간을 보장하지 않습니다.

## 검증 결과 (2026-11-09)
- Unity 6000.0.50f1 Windows x64 빌드 성공.
- 최종 Windows 실행 파일 자동 검증 종료 코드 0 / FOREST_SMOKE_PASS.
- 10개 수집물 및 순찰 경로 접근성, 벽 시야 차단, 출구 잠금, 중복 수집 방지 확인.
- 10초 지연 스폰, 실제 출구 승리, 추격 진입/해제, 접촉 실패, 승리·실패 후 씬 재시작 초기화 확인.
- 숨김 창 ScreenCapture는 검은 화면을 반환하여 Camera.Render로 배경을 별도 확인했습니다.
- 배경 캡처: Builds/Windows/ForestPreview.png (HUD 미포함).
- 손으로 조작하는 전체 플레이, HUD의 시각 배치, 실제 소리의 체감 크기와 5~8분 난이도는 별도 확인이 필요합니다.
- 배포할 때는 exe 하나가 아니라 Builds/Windows 폴더 전체를 전달하세요.


## 괴물 시야·무작위 이동 변경 (2026-11-26)
- 시야는 표시하지 않으며, 전방 120도·12m 이내에서 벽 등으로 가려지지 않은 플레이어를 발견하면 추격합니다.
- 추격 중에도 동일한 시야 조건을 적용합니다. 시야에서 벗어나면 즉시 무작위 배회로 전환합니다.
- 주변 14m 내 NavMesh에서 도달 가능한 목적지를 선택하고, 도착하면 다시 선택합니다. 유효 지점이 없으면 1초 후 재시도합니다.
- Windows 빌드 갱신 및 실행 파일 자동 검증 통과(종료 코드 0, FOREST_SMOKE_PASS).
- 정면 감지, 후방·거리 초과 시 즉시 추격 해제, 벽 가림, 재발견 추격, 무작위 이동·목적지 재선택 확인. 기존 스태미나·수집·승패·재시작 검증도 통과했습니다.
