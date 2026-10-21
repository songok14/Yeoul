# 테스트 서명 키

`ci-test.keystore`는 **테스트 APK 전용**이다. 매번 같은 키로 서명해야 폰에 깔린 앱을 지우지 않고 새 APK로 덮어 설치할 수 있다.

- 별칭 `hanjeopsi`, 비밀번호 `hanjeopsi-test` (레포에 공개된 값이라 보안이 없다)
- GitHub Actions가 `GODOT_ANDROID_KEYSTORE_RELEASE_*` 환경 변수로 이 키를 넘긴다
- 스토어에 올릴 일이 생기면 새 키를 만들고 GitHub 비밀값으로만 넣는다
