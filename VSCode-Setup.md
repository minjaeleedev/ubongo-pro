# VS Code로 Unity 개발하기

## 🚀 빠른 시작

### 1. 필수 소프트웨어 설치
```bash
# Unity Hub 설치 (공식 사이트에서 다운로드)
# https://unity.com/download

# .NET SDK 설치
brew install --cask dotnet-sdk

# Mono 설치 (선택사항, 디버깅용)
brew install mono
```

### 2. Unity 프로젝트 생성
```bash
# Unity Hub CLI로 프로젝트 생성 (Unity Hub 설치 후)
# 또는 Unity Hub GUI에서 생성

# 이 폴더를 Unity에서 열기
open -a "Unity Hub"
# 그다음 "Add" → 이 폴더 선택
```

### 3. VS Code 확장 프로그램 설치
```bash
# VS Code 열기
code .

# 추천 확장 프로그램 자동 설치 프롬프트가 나타남
# 또는 수동으로 설치:
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.csdevkit
code --install-extension visualstudiotoolsforunity.vstuc
code --install-extension unity.unity-debug
```

### 4. Unity 에디터 설정
1. Unity 에디터 열기
2. Unity → Preferences (Mac) / Edit → Preferences (Windows)
3. External Tools → External Script Editor
4. "Visual Studio Code" 선택

## 📝 VS Code에서 작업하기

### IntelliSense 활성화
1. Unity 에디터에서 프로젝트 열기
2. Assets → Open C# Project (자동으로 .sln 파일 생성)
3. VS Code가 자동으로 열림
4. OmniSharp 서버가 시작되면 IntelliSense 사용 가능

### 코드 작성 팁
- `Ctrl+Shift+P` → "OmniSharp: Restart" (문제 발생 시)
- `Cmd+.` (Mac) / `Ctrl+.` (Win) → Quick Fix
- `F12` → Go to Definition
- `Shift+F12` → Find All References

### 디버깅
1. Unity 에디터에서 Play Mode 실행
2. VS Code에서 `F5` 또는 디버그 패널에서 "Unity Editor" 선택
3. Breakpoint 설정 후 디버깅

## 🎮 VS Code 작업 흐름

### 1. 새 스크립트 생성
```bash
# VS Code 터미널에서
touch Assets/Scripts/MyNewScript.cs
```

### 2. 스크립트 템플릿
```csharp
using UnityEngine;

namespace Ubongo
{
    public class MyNewScript : MonoBehaviour
    {
        private void Start()
        {
            // 초기화 코드
        }
        
        private void Update()
        {
            // 매 프레임 실행
        }
    }
}
```

### 3. Unity 에디터와 동기화
- 파일 저장 시 Unity가 자동으로 컴파일
- Unity 콘솔에서 에러 확인
- VS Code Problems 패널에서도 에러 확인 가능

## 🛠 문제 해결

### IntelliSense가 작동하지 않을 때
1. `.sln` 파일이 있는지 확인
2. 없다면 Unity에서 "Assets → Open C# Project"
3. VS Code 재시작
4. OmniSharp 로그 확인 (Output 패널)

### Unity 에디터와 연결이 안 될 때
1. Unity Editor 설정에서 External Script Editor 확인
2. VS Code 경로가 올바른지 확인
   - Mac: `/Applications/Visual Studio Code.app`
   - Windows: `C:\Users\{username}\AppData\Local\Programs\Microsoft VS Code\Code.exe`

### 컴파일 에러
1. Unity 콘솔 확인 (더 자세한 에러 메시지)
2. Assembly Definition 파일 확인
3. 네임스페이스 충돌 확인

## 📚 유용한 단축키

| 기능 | Mac | Windows |
|------|-----|---------|
| Quick Fix | Cmd+. | Ctrl+. |
| Go to Definition | F12 | F12 |
| Find References | Shift+F12 | Shift+F12 |
| Rename Symbol | F2 | F2 |
| Format Document | Shift+Option+F | Shift+Alt+F |
| Comment Line | Cmd+/ | Ctrl+/ |
| Open Terminal | Ctrl+` | Ctrl+` |

## 🔗 추가 리소스
- [Unity VS Code 통합 가이드](https://code.visualstudio.com/docs/other/unity)
- [OmniSharp 문서](https://www.omnisharp.net/)
- [Unity 스크립팅 API](https://docs.unity3d.com/ScriptReference/)