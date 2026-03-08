# 業務タスク管理アプリ

WPF (.NET Framework 4.8) で作成した業務用タスク管理アプリケーションです。

## 主な機能

### タスク管理
- 1日の初めにタスクを順序立てて登録
- タスクの並び替え（上下移動）
- カテゴリ・メモの設定
- 予定時間の入力（分数指定 or 開始/終了時刻から自動計算）

### タイマー計測
- **▶ 開始** ボタンでタイマー開始 → 実績時間を自動計測
- **⏸ 停止** で一時停止（累積時間を保持）
- **✓ 完了** で実績を確定
- **↺ リセット** でタイマーをリセット

### 予実比較
- 各タスクに予定・実績・差異を表示
- 1日の合計予定・実績をフッターに表示

### レポート
- 週次・月次・カスタム期間の集計
- タスク一覧（日付・カテゴリ・予定・実績・差異・状態）
- カテゴリ別集計

## セットアップ

### 必要環境
- Windows 10 以降
- .NET Framework 4.8
- Visual Studio 2019 以降（推奨）

### ビルド手順

```powershell
# NuGet パッケージを復元
nuget restore TaskManager.sln

# ビルド
msbuild TaskManager.sln /p:Configuration=Release
```

または Visual Studio で `TaskManager.sln` を開いてビルド・実行。

### データ保存先
アプリのデータは以下に自動作成されます：
```
%APPDATA%\TaskManager\tasks.db
```

## 使い方

1. アプリを起動すると本日の日付が表示されます
2. **タスクを追加** フォームにタスク名を入力して「追加」ボタン（またはEnterキー）
3. 予定開始・終了時刻を入力すると予定分数が自動計算されます
4. タスクの **▶ 開始** ボタンを押すとタイマーが動き始めます
5. 完了したら **✓ 完了** を押して実績を記録
6. 右上の **レポート** ボタンで週次・月次の予実集計を確認

## プロジェクト構成

```
TaskManager/
├── Models/
│   ├── TaskItem.cs         # タスクエンティティ
│   └── TaskStatus.cs       # ステータス列挙型
├── Services/
│   └── DatabaseService.cs  # SQLite CRUD
├── ViewModels/
│   ├── BaseViewModel.cs    # INotifyPropertyChanged 基底
│   ├── RelayCommand.cs     # ICommand 実装
│   ├── TaskItemViewModel.cs # タスク1件のVM（タイマー含む）
│   ├── MainViewModel.cs    # メイン画面VM
│   └── ReportViewModel.cs  # レポートVM
├── Views/
│   └── ReportWindow.xaml   # レポートウィンドウ
├── Converters/
│   ├── StatusToColorConverter.cs
│   ├── BoolToVisibilityConverter.cs
│   ├── InverseBoolConverter.cs
│   ├── MinutesToTimeStringConverter.cs
│   └── NullToVisibilityConverter.cs
├── App.xaml                # グローバルスタイル・リソース
└── MainWindow.xaml         # メイン画面
```
