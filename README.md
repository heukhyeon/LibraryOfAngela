# Library Of Angela 

Library of Ruina의 모드 프레임워크

## 적용 방법
Release 에서의 최신 버전을 다운 받습니다.
개발하는 모드의 Assemblies 폴더에 Assemblies.zip 을 압축 해제합니다. 아래와 같은 구성이 되야합니다.
- Mod
  - Assemblies
    - LoALoader.dll
    - LazyDll
      - LoAInterface.dll
      - LoARuntime.dll
      - LoARuntimeUI.dll
      - loa_ui
      - LoAAsset
      - LoAAsset_UI
      - (그 외 개발하시는 dll)

## 적용 관련 필수 준수사항
- LoAInterface.dll 과 LoARuntime.dll 은 반드시 Assemblies 내 LazyDll 폴더에 들어가야합니다. Assemblies 폴더 하위에 직접 두고 실행시 오류가 발생합니다.
- LoARuntime은 일반 모드의 dll에서 참조하면 안됩니다.
- 일반적으로 모더가 직접 Visual Studio에서 빌드하는 dll은  LoAInterface.dll 을 참조하므로 LazyDll에 들어가야합니다. 단, 작업하신 dll이 LoAInterface 외에 추가로 참조한 dll이 있다면 그것들은 LazyDll에 들어가도 되고 Assemblies에 직접 포함되어도 상관없습니다.

 

## 버전 규칙
`x.y.z.a`
### 메이저 업데이트 (x)
LoALoader.dll 의 동작이 변경됨. 거의 반드시 기존 모드들의 충돌이 발생합니다. 모든 모드가 최신 버전으로 Loader를 업데이트 해야합니다.

### 마이너 업데이트 (y)
LoAInterface.dll의 기존 시그니처가 변경되거나 삭제됨. 해당 기능을 사용하는 기존 모드들의 경우 충돌이 발생할 수 있습니다.

### 빌드 업데이트 (z)
LoAInterface.dll에 신규 인터페이스나 신규 메소드가 추가됨. 기존 모드들은 충돌이 발생하지 않습니다.

### 리비전 업데이트 (a)
LoARuntime.dll 내부 변경. 기존 모드들은 충돌이 발생하지 않습니다.
