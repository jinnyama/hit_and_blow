using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CPUEasyScript : MonoBehaviour
{


    // --- UIコンポーネント (Inspectorから設定) ---
    public Text PlayerInputText; // プレイヤーの推測履歴とその結果を表示
    public Text CPUInputText;    // CPUの推測履歴とその結果を表示
    public Text PlayerBaseText;  // プレイヤーの秘密の数字を表示 (CPUが当てるターゲット)
    public Text CPUBaseText;     // CPUの秘密の数字を表示 (プレイヤーが当てるターゲット)

    // --- ゲームで使用する変数 ---
    private int playerGuessCount = 0;
    private int cpuGuessCount = 0;

    // CPU vs プレイヤー (CPUのターゲット)
    private int[] playerBaseNumber = new int[3];
    private int lastCPUGuess = 0;

    // 既存のAIロジック用変数は使用しないため、ここでは残しますが、ロジック内で無視します。
    private List<int> possibleGuesses = new List<int>();

    // プレイヤー vs CPU (プレイヤーのターゲット)
    private int[] cpuBaseNumber = new int[3];

    // 汎用
    private int[] tempDigits = new int[3];
    private int[] currentGuessDigits = new int[3]; // CPU推測の桁分解用
    public string gameMode = "Setup"; // "Setup", "PlayerTurn", "CPUTurn", "GameOver"

    // TextScript (プレイヤーからの入力を取得するスクリプト)への参照
    public TextScript textScript;

    // --- Unityライフサイクルメソッド ---

    void Start()
    {
        if (textScript == null)
        {
            textScript = GameObject.Find("InputText").GetComponent<TextScript>();
        }

        // 秘密の数字の生成
        GenerateCPUBaseNumber();

        // AIロジックはイージーモードなので初期化のみ（高度な絞り込みは行わない）
        InitializeNewCPU_EasyMode();

        // 初期メッセージをセット
        PlayerInputText.text = "【プレイヤー 試行履歴 (CPUの数字を当てる)】\n";
        CPUInputText.text = "【CPU 試行履歴 (あなたの数字を当てる)】\n";
        PlayerBaseText.text = "あなたの秘密の数字: 未設定";
        CPUBaseText.text = "CPUの秘密の数字: ???";

        // 最初の指示メッセージ
        textScript.Inputtext.text = $"あなたの秘密の3桁の数字（重複なし）を入力し、Enterで決定してください。\n {textScript.PlayerInput}";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (gameMode == "Setup")
            {
                HandlePlayerBaseInput();
            }
            else if (gameMode == "PlayerTurn")
            {
                HandlePlayerGuess();
            }
            else if (gameMode == "CPUTurn")
            {
                // EnterキーでCPUの推測結果を確認した後、プレイヤーのターンに戻る
                gameMode = "PlayerTurn";
            }
            textScript.PlayerInput = 0;
        }
        if (gameMode == "CPUTurn")
        {
            // EnterキーでCPUの推測結果を確認した後、プレイヤーのターンに戻る
            gameMode = "PlayerTurn";
        }
        // --- 入力プロンプト表示 ---
        if (gameMode == "Setup")
        {
            textScript.Inputtext.text = $"あなたの秘密の3桁の数字（重複なし）を入力し、Enterで決定してください。\n {textScript.PlayerInput}";
        }
        else if (gameMode == "PlayerTurn")
        {
            textScript.Inputtext.text = $"--- あなたの推測ターン！ ({playerGuessCount + 1}回目) ---\n";
            textScript.Inputtext.text += " CPUの秘密の数字を推測し、Enterを押してください。\n";
            textScript.Inputtext.text += textScript.PlayerInput.ToString();
        }
        else if (gameMode == "CPUTurn")
        {
            textScript.Inputtext.text = $"--- CPUの推測結果を表示しました。 ---\n Enterを押して、あなたのターンに戻ってください。";
        }
        
       
           
        if (Input.GetKeyDown(KeyCode.R))
        {
             SceneManager.LoadScene("Title");
        }
        
    }

    // --- 初期設定/ベースナンバー生成 ---

    private void HandlePlayerBaseInput()
    {
        int playerInput = textScript.PlayerInput;

        if (InputCheckAndSetBaseDigits(playerInput, playerBaseNumber))
        {
            gameMode = "PlayerTurn";
            PlayerBaseText.text = $"あなたの秘密の数字: {playerInput:D3}";
            textScript.PlayerInput = 0;
            PlayerInputText.text += $"秘密の数字 {playerInput:D3} を設定しました。\n";
        }
    }

    private void GenerateCPUBaseNumber()
    {
        List<int> availableNumbers = Enumerable.Range(0, 10).ToList();
        for (int i = 0; i < 3; i++)
        {
            int randIndex = Random.Range(0, availableNumbers.Count);
            cpuBaseNumber[i] = availableNumbers[randIndex];
            availableNumbers.RemoveAt(randIndex);
        }
    }

    // --- プレイヤーの推測ターン ---

    private void HandlePlayerGuess()
    {
        int playerInput = textScript.PlayerInput;

        if (InputCheckAndSetBaseDigits(playerInput, tempDigits))
        {
            playerGuessCount++;
            var result = CheckHitBlow(tempDigits, cpuBaseNumber);

            // プレイヤー推測履歴の出力
            string historyLine = $"\n試行 {playerGuessCount}: **{playerInput:D3}** -> 判定: **{result.hit}ヒット {result.blow}ブロー**";
            PlayerInputText.text += historyLine;

            if (result.hit == 3)
            {
                GameClear(playerInput);
            }
            else
            {
                // プレイヤーのターンが終わったらCPUのターンへ移行し、CPUの処理を実行
                HandleCPUFeedback_EasyMode(); // ★★★ イージーモードのCPU処理を実行 ★★★
                gameMode = "CPUTurn";
            }
        }
    }

    private void GameClear(int correctGuess)
    {
        gameMode = "GameOver";
        PlayerInputText.text = $"\n\n🎉 **Game Clear!!** {correctGuess:D3} で正解です！\nあなたは**{playerGuessCount}回目**で正解しました！\n";
        CPUBaseText.text = $"CPUの秘密の数字: {cpuBaseNumber[0]}{cpuBaseNumber[1]}{cpuBaseNumber[2]}";
        CPUInputText.text += "\n--- プレイヤーの勝利によりCPUの推測は中断しました ---";
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("Title");
        }
    }

    // --- CPUの推測ターン (イージーモード) ---

    /// <summary>
    /// CPUのAIロジックを初期化します。（イージーモード）
    /// </summary>
    private void InitializeNewCPU_EasyMode()
    {
        // 初回推測の準備
        lastCPUGuess = CPU_EasyGuess();
        SetDigitsFromArray(lastCPUGuess, currentGuessDigits); // currentGuessDigits に最初の推測を格納
    }

    /// <summary>
    /// 難易度を下げたCPUの推測ロジック。完全にランダムな重複しない3桁の数字を生成します。
    /// </summary>
    private int CPU_EasyGuess()
    {
        List<int> digits = Enumerable.Range(0, 10).ToList();

        // シャッフルして最初の3つを選ぶことで、重複しないランダムな3桁を保証
        List<int> shuffledDigits = digits.OrderBy(_ => Random.value).ToList();

        // 最初の3つの数字を使って推測を構成
        int guess = shuffledDigits[0] * 100 + shuffledDigits[1] * 10 + shuffledDigits[2];
        return guess;
    }

    /// <summary>
    /// プレイヤーからのフィードバックを受け取り、次の推測を生成します。（イージーモード版）
    /// </summary>
    private void HandleCPUFeedback_EasyMode()
    {
        cpuGuessCount++;

        // 前回推測に対する判定を取得
        var feedback = CheckHitBlow(currentGuessDigits, playerBaseNumber);
        int hit = feedback.hit;
        int blow = feedback.blow;

        // 判定結果をCPUInputTextに追記
        string feedbackLine = $" -> プレイヤー判定: **{hit}ヒット {blow}ブロー** ";
        CPUInputText.text += feedbackLine;

        if (hit == 3)
        {
            EndGame(lastCPUGuess);
            return;
        }

        // ★★★ 難易度を下げたロジック：フィードバックを無視し、完全にランダムに推測する ★★★
        lastCPUGuess = CPU_EasyGuess();

        if (lastCPUGuess != 0)
        {
            // currentGuessDigits に次の推測を格納
            SetDigitsFromArray(lastCPUGuess, currentGuessDigits);

            // CPUの推測内容をCPUInputTextに追記
            CPUInputText.text += $"\n試行 {cpuGuessCount}: **{lastCPUGuess:D3}** を推測しました。";
        }
        else
        {
            // エラー処理（理論上は起こらないが念のため）
            CPUInputText.text += "\nエラー: ランダム推測に失敗しました。";
        }
    }


    // --- 汎用ロジック補助関数 ---

    private bool InputCheckAndSetBaseDigits(int num, int[] targetArray)
    {
        if (num < 0 || num > 999)
        {
            PlayerInputText.text += $"\nエラー: 000から999までの3桁の数字を入力してください。";
            return false;
        }
        SetDigitsFromArray(num, tempDigits);
        int d0 = tempDigits[0];
        int d1 = tempDigits[1];
        int d2 = tempDigits[2];
        if (d0 == d1 || d1 == d2 || d2 == d0)
        {
            PlayerInputText.text += $"\nエラー: 数字は重複しないようにしてください。";
            return false;
        }
        targetArray[0] = d0;
        targetArray[1] = d1;
        targetArray[2] = d2;
        return true;
    }

    private (int hit, int blow) CheckHitBlow(int[] guessArray, int[] baseArray)
    {
        int hit = 0;
        int blow = 0;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (guessArray[i] == baseArray[j])
                {
                    if (i == j) { hit++; }
                    else { blow++; }
                    break;
                }
            }
        }
        return (hit, blow);
    }

    private void SetDigitsFromArray(int num, int[] array)
    {
        array[0] = num / 100;
        array[1] = (num / 10) % 10;
        array[2] = num % 10;
    }

    // --- ゲーム終了ロジック ---

    private void EndGame(int correctGuess)
    {
        gameMode = "GameOver";
        CPUInputText.text = $"\n\n🎉 **CPUの勝利!!** 秘密の数字 {correctGuess:D3} を当てました！\nCPUは**{cpuGuessCount}回目**で正解しました！\n";
        PlayerBaseText.text = $"あなたの秘密の数字: {playerBaseNumber[0]}{playerBaseNumber[1]}{playerBaseNumber[2]}";
        PlayerInputText.text += "\n\n--- CPUの勝利によりゲーム終了 ---";
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("Title");
        }
    }

    // --- 旧AIロジックは使用しないためそのまま残す (今回は削除対象ではないため) ---

    private void GenerateAllPossibleNumbers()
    {
        // この関数はイージーモードでは使用しません。
    }

    private void FilterPossibleGuesses(int guess, int hit, int blow)
    {
        // この関数はイージーモードでは使用しません。
    }

}
