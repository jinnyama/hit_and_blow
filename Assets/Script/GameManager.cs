using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Random = UnityEngine.Random; // UnityEngine.Randomを使用することを明示

public class GameManagerScript : MonoBehaviour
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
    private List<int> possibleGuesses = new List<int>(); // CPU推測ロジック用

    // プレイヤー vs CPU (プレイヤーのターゲット)
    private int[] cpuBaseNumber = new int[3];

    // 汎用
    private int[] tempDigits = new int[3];
    private int[] currentGuessDigits = new int[3];//謎
    public string gameMode = "Setup"; // "Setup", "PlayerTurn", "CPUTurn", "GameOver"

    // TextScript (プレイヤーからの入力を取得するスクリプト)への参照
    public TextScript gameManagerScript;

    // --- Unityライフサイクルメソッド ---

    void Start()
    {
        if (gameManagerScript == null)
        {
            gameManagerScript = GameObject.Find("InputText").GetComponent<TextScript>();
        }

        // 秘密の数字の生成
        GenerateCPUBaseNumber(); // プレイヤーが当てる数字
        GenerateAllPossibleNumbers(); // CPUの推測候補リスト

        // 初期メッセージをセット
        PlayerInputText.text = "【プレイヤー 試行履歴 (CPUの数字を当てる)】\n";
        CPUInputText.text = "【CPU 試行履歴 (あなたの数字を当てる)】\n";
        PlayerBaseText.text = "あなたの秘密の数字: 未設定";
        CPUBaseText.text = "CPUの秘密の数字: ???";


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
                HandleCPUFeedback();
            }
            gameManagerScript.PlayerInput = 0;
        }
    }

    // --- 初期設定/ベースナンバー生成 ---

    /// <summary>プレイヤーの秘密の数字を設定します。</summary>
    private void HandlePlayerBaseInput()
    {
        int playerInput = gameManagerScript.PlayerInput;

        if (InputCheckAndSetBaseDigits(playerInput, playerBaseNumber))
        {
            // 秘密の数字を設定完了
            gameMode = "PlayerTurn";
            PlayerBaseText.text = $"あなたの秘密の数字: {playerInput:D3}";



            // 初期推測値をリセット（もしあれば）
            gameManagerScript.PlayerInput = 0;
        }
    }

    /// <summary>重複しない3桁の秘密の数字を生成します（0-9）。</summary>
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

    /// <summary>プレイヤーの推測を処理し、判定結果をPlayerInputTextに表示します。</summary>
    private void HandlePlayerGuess()
    {
        int playerInput = gameManagerScript.PlayerInput;
        // gameManagerScript.PlayerInput = 0; 

        if (InputCheckAndSetBaseDigits(playerInput, tempDigits))
        {
            playerGuessCount++;
            var result = CheckHitBlow(tempDigits, cpuBaseNumber);

            // ★★★ プレイヤー推測履歴の出力 ★★★
            string historyLine = $"\n試行 {playerGuessCount}: **{playerInput:D3}** -> 判定: **{result.hit}ヒット {result.blow}ブロー**";
            PlayerInputText.text += historyLine;

            if (result.hit == 3)
            {
                GameClear(playerInput);
            }
            else
            {
                // プレイヤーのターンが終わったらCPUのターンへ移行
                gameMode = "CPUTurn";
                //CPUInputText.text += $"\n--- CPUの推測ターン開始 (Enterで進行) ---";
                CPU_Guess(); // CPUの初回推測を実行
            }
        }
    }

    /// <summary>プレイヤーが3 Hitを達成した場合に呼び出されます。</summary>
    private void GameClear(int correctGuess)
    {
        gameMode = "GameOver";

        // ★★★ GameClear表示 ★★★
        PlayerInputText.text = $"\n\n🎉 **Game Clear!!** {correctGuess:D3} で正解です！\nあなたは**{playerGuessCount}回目**で正解しました！\n";
        CPUBaseText.text = $"CPUの秘密の数字: {cpuBaseNumber[0]}{cpuBaseNumber[1]}{cpuBaseNumber[2]}";

        // CPUの秘密の数字が明かされたので、CPUのターンは中断
        CPUInputText.text = "";//"\n--- プレイヤーの勝利によりCPUの推測は中断しました ---";
    }

    // --- CPUの推測ターン ---

    /// <summary>CPUの推測を処理し、判定結果をCPUInputTextに表示します。</summary>
    private void HandleCPUFeedback()
    {
        var feedback = CheckHitBlow(currentGuessDigits, playerBaseNumber); // 前回推測に対する判定
        int hit = feedback.hit;
        int blow = feedback.blow;

        // ★★★ 判定結果をCPUInputTextに追記 ★★★
        cpuGuessCount++;
        string feedbackLine = $"{lastCPUGuess} -> プレイヤー判定: **{hit}ヒット {blow}ブロー** (候補数: {possibleGuesses.Count})\n";
        CPUInputText.text += feedbackLine;

        if (hit == 3)
        {
            EndGame(lastCPUGuess);
            return;
        }

        // 可能性のある数字リストを絞り込む
        FilterPossibleGuesses(lastCPUGuess, hit, blow);

        // 次の推測へ
        if (possibleGuesses.Count > 0)
        {
            CPU_Guess();
            gameMode = "PlayerTurn"; // CPUのターンが終わったらプレイヤーのターンへ
            //PlayerInputText.text += $"\n--- あなたの推測ターン (Enterで進行) ---";
        }
        else
        {
            CPUInputText.text = "\nエラー: 可能な推測がなくなりました。プレイヤーの判定が間違っているか、ロジックエラーです。";
            gameMode = "GameOver";
        }
    }

    /// <summary>CPUが次の推測を行い、CPUInputTextに履歴として表示します。</summary>
    private void CPU_Guess()
    {


        // リストの最初の数字を推測として採用
        lastCPUGuess = possibleGuesses[0];

        // 各桁を currentGuessDigits に格納
        SetDigitsFromArray(lastCPUGuess, currentGuessDigits);

        // ★★★ CPUの推測内容をCPUInputTextに追記 ★★★
        //CPUInputText.text += $"\n試行 {cpuGuessCount}: **{lastCPUGuess:D3}** を推測しました。";
    }

    // --- 汎用ロジック補助関数 ---

    /// <summary>
    /// 入力チェックを行い、有効な場合は指定された配列に各桁を格納します。
    /// </summary>
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

    /// <summary>
    /// 2つの3桁の数字配列を比較し、HitとBlowの数を返します。
    /// </summary>
    private (int hit, int blow) CheckHitBlow(int[] guessArray, int[] baseArray)
    {
        int hit = 0;
        int blow = 0;

        for (int i = 0; i < 3; i++) // 推測 (guessArray)
        {
            for (int j = 0; j < 3; j++) // 秘密の数字 (baseArray)
            {
                if (guessArray[i] == baseArray[j])
                {
                    if (i == j)
                    {
                        hit++; // Hitの数を１プラス
                    }
                    else
                    {
                        blow++; // Blowの数を１プラス
                    }
                    break;
                }
            }
        }
        return (hit, blow);
    }

    /// <summary>整数を3桁に分解して配列に格納します。</summary>
    private void SetDigitsFromArray(int num, int[] array)
    {
        array[0] = num / 100;
        array[1] = (num / 10) % 10;
        array[2] = num % 10;
    }

    // --- CPU AIロジック ---

    private void GenerateAllPossibleNumbers()
    {
        possibleGuesses.Clear();
        for (int i = 0; i <= 9; i++)
        {
            for (int j = 0; j <= 9; j++)
            {
                for (int k = 0; k <= 9; k++)
                {
                    if (i != j && i != k && j != k)
                    {
                        possibleGuesses.Add(i * 100 + j * 10 + k);
                    }
                }
            }
        }
    }

    private void FilterPossibleGuesses(int guess, int hit, int blow)
    {
        List<int> newPossibleGuesses = new List<int>();
        int[] guessDigits = new int[3];
        SetDigitsFromArray(guess, guessDigits);

        foreach (int potential in possibleGuesses)
        {
            int[] potentialDigits = new int[3];
            SetDigitsFromArray(potential, potentialDigits);

            var result = CheckHitBlow(guessDigits, potentialDigits);

            if (result.hit == hit && result.blow == blow)
            {
                newPossibleGuesses.Add(potential);
            }
        }
        possibleGuesses = newPossibleGuesses;
    }

    /// <summary>CPUが3 Hitを達成した場合に呼び出されます。</summary>
    private void EndGame(int correctGuess)
    {
        gameMode = "GameOver";

        // CPUの勝利メッセージをCPUInputTextに追記
        CPUInputText.text = $"\n\n🎉 **CPUの勝利!!** 秘密の数字 {correctGuess:D3} を当てました！\nCPUは**{cpuGuessCount}回目**で正解しました！\n";
        PlayerBaseText.text = $"あなたの秘密の数字: {playerBaseNumber[0]}{playerBaseNumber[1]}{playerBaseNumber[2]}";

        PlayerInputText.text += "\n\n--- CPUの勝利によりゲーム終了 ---";
    }

}