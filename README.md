https://img.shields.io/github/issues/Nao6165/TimeCardReminder
[![GitHub license](https://img.shields.io/github/license/Nao6165/TimeCardReminder)](https://github.com/Nao6165/TimeCardReminder/blob/master/LICENSE)
# TimeCardReminder
リマインドしたい内容と時間をセットしておくとメッセージボックスでお知らせしてくれるツールです。
コロナ禍によって在宅勤務が増え、忘れがちなタイムカード操作のリマインドアプリを作ってみました。
通知メッセージや時刻は自分で設定可能なので、カード忘れ以外の案件にもリマインドツールとして活用できます。

![Img_MainWindow_v111_01](https://user-images.githubusercontent.com/54632092/90096300-de5c8300-dd6d-11ea-8a9f-7945ea2b6528.PNG)
![Img_Message_03](https://user-images.githubusercontent.com/54632092/90096676-e10ba800-dd6e-11ea-8167-734f9ce09911.png)
# 動作条件
下記環境にて動作確認
- OS
  windows10 Pro
- CPU
  Core i5-4310M 2.70GHz
- RAM
  8.00GB
# 使用言語、環境
## 開発環境
WPF Project in VisualSutudio 2019 C#
# 使い方
## デプロイ方法
1. [VisualStudio2019](https://visualstudio.microsoft.com/ja/downloads/)をインストール
2. 適当なフォルダに当リポジトリをクローン
   ```
   git clone https://github.com/Nao6165/TimeCardReminder.git
   ```
3. TimeCardReminder.slnからVisualStudio2019を起動してビルドを実行
## 使用方法
### 簡単な使用手順
1. 当アプリを起動。(タスクトレイに常駐します)
2. タスクトレイのアイコンを右クリックし、**設定**を選択。(設定画面が起動します)
3. 通知したい**メッセージ**と**時刻**を設定
4. **追加ボタン**を押下
5. チェックボックスにチェックが入っていることを確認した上で**設定ボタン**を押下
![設定方法](https://user-images.githubusercontent.com/54632092/87239912-56721900-c44f-11ea-9121-01503b85c456.gif)
## 活用法紹介
一度、リマインド設定を行い、TimeCardReminder.exeをスタートアップに登録しておけば、あとはPCの電源投入時に自動で起動し、自動でリマインドしてくれます。
# ライセンス
MIT
