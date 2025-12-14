using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Random = UnityEngine.Random;
using UnityEngine.UIElements;// UnityEngine.Randomを使用することを明示
using UnityEngine.SceneManagement;


public class VSPlayer1Script : MonoBehaviour
{

    // --- UIコンポーネント (Inspectorから設定) ---
    public Text PlayerInputText; // プレイヤーの推測履歴とその結果を表示
    public Text CPUInputText;    // CPUの推測履歴とその結果を表示
    public Text PlayerBaseText;  // あなたの秘密の数字を表示 (CPUが当てるターゲット)
    public Text CPUBaseText;     // CPUの秘密の数字を表示 (プレイヤーが当てるターゲット)

    // --- ゲームで使用する変数 ---
    private int playerGuessCount = 0;
    private int cpuGuessCount = 0;
    private const int maxCount = 10;

    // Player 1 (あなた) のターゲット
    private int[] playerBaseNumber = new int[3];

    // Player 2 (CPU) のターゲット
    private int[] cpuBaseNumber = new int[3];

    // CPU AI ロジック用
    private int lastCPUGuess = 0;
    private List<int> possibleGuesses = new List<int>(); // 720通りの候補リスト (全体)
    private List<int> availableDigits = new List<int>(); // まだ排除されていない数字 (0-9)
    private bool useCombinatorialGuessing = false; // しらみつぶしモードフラグ

    // 汎用
    private int[] tempDigits = new int[3];

    // ゲームモード管理: Setup_P1 -> PlayerTurn -> CPUTurn -> GameOver
    public string gameMode = "Setup_P1";

    // TextScript (プレイヤーからの入力を取得するスクリプト)への参照
    public TextScript textScript;

    // --- Unityライフサイクルメソッド ---

    void Start()
    {
        if (textScript == null)
        {
            textScript = GameObject.Find("InputText").GetComponent<TextScript>();
        }

        // 秘密の数字の生成 (CPUのターゲット)
        GenerateCPUBaseNumber();

        // AIロジックの初期化
        for (int i = 0; i <= 9; i++) { availableDigits.Add(i); }
        GenerateAllPossibleNumbers(); // 720通りの候補リストを生成

        // 初期メッセージをセット
        PlayerInputText.text = "【Player 1 試行履歴 (CPUの数字を当てる)】\n";
        CPUInputText.text = "【Player 2試行履歴 (あなたの数字を当てる)】\n";
        PlayerBaseText.text = "あなたの秘密の数字: 未設定";
        CPUBaseText.text = "Player 2の秘密の数字: ???";

        // 最初の指示メッセージ
        SetInputPrompt($"Player 1: 秘密の3桁の数字（重複なし）を入力し、Enterで決定してください。");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (gameMode == "Setup_P1")
            {
                HandlePlayerBaseInput();
            }
            else if (gameMode == "PlayerTurn")
            {
                HandlePlayerGuess();
            }
            else if (gameMode == "CPUTurn") // P1の推測後、EnterキーでCPUのターンを**開始**
            {
                HandleCPUTurn();
            }
        }

        // プロンプトを更新
        UpdateInputTextDisplay();

        if (playerGuessCount >= maxCount && cpuGuessCount >= maxCount && gameMode != "GameOver")
        {
            DrawGame();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
                SceneManager.LoadScene("Title");
        }
        
    }

    /// <summary>現在のゲームモードに基づき、UIのプロンプトを更新します。</summary>
    private void UpdateInputTextDisplay()
    {
        string prompt = GetCurrentPromptMessage();
        SetInputPrompt(prompt);
    }

    /// <summary>TextScriptを通して入力プロンプトを設定します。</summary>
    private void SetInputPrompt(string message)
    {
        if (textScript != null && textScript.Inputtext != null)
        {
            string currentInput = textScript.PlayerInput.ToString("D3");
            if (gameMode != "GameOver")
            {
                textScript.Inputtext.text = $"{message}\n現在の入力: {currentInput}";
            }
            else
            {
                textScript.Inputtext.text = message;
            }
        }
    }

    // --- 初期設定/ベースナンバー生成 ---

    /// <summary>Player 1 (あなた) の秘密の数字を設定します。</summary>
    private void HandlePlayerBaseInput()
    {
        int playerInput = textScript.PlayerInput;

        if (InputCheckAndSetBaseDigits(playerInput, playerBaseNumber))
        {
            // 成功したらゲーム開始
            gameMode = "PlayerTurn";
            PlayerBaseText.text = $"あなたの秘密の数字: {playerInput:D3}";
            PlayerInputText.text += $"秘密の数字 {playerInput:D3} を設定しました。\n";

            textScript.PlayerInput = 0; // 入力値クリア

            // ゲーム開始プロンプト
            SetInputPrompt($"--- Player 1の推測ターン！ (1回目) ---");
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
        int playerInput = textScript.PlayerInput;
        textScript.PlayerInput = 0;

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
                SetInputPrompt($"--- CPUの推測ターン開始 (Enterを押して進行) ---");
            }
        }
    }

    // --- CPUの推測ターン ---

    /// <summary>CPUのターン（判定と推測）を実行し、判定結果をCPUInputTextに表示します。</summary>
    private void HandleCPUTurn()
    {
        if (cpuGuessCount > 0)
        {
            // 1. 前回の推測に対する判定を取得
            var feedback = CheckHitBlow(tempDigits, playerBaseNumber); // 前回推測に対する判定
            int hit = feedback.hit;
            int blow = feedback.blow;

            // ★★★ 判定結果をCPUInputTextに追記 ★★★
            string feedbackLine = $" -> 判定: **{hit}ヒット {blow}ブロー** (候補数: {possibleGuesses.Count})";
            CPUInputText.text += feedbackLine;

            if (hit == 3)
            {
                EndGame(lastCPUGuess); // CPUの勝利
                return;
            }

            // 2. AIロジックによる候補の絞り込み
            FilterPossibleGuesses(lastCPUGuess, hit, blow);

            // ★★★ 新しいAIロジックの追加 (0H0Bによる数字排除) ★★★
            FilterAvailableDigits(lastCPUGuess, hit, blow);
        }

        // 3. 次の推測へ
        if (possibleGuesses.Count > 0)
        {
            CPU_Guess(); // 次の推測を実行

            // CPUのターンが終わったらプレイヤーのターンへ移行
            gameMode = "PlayerTurn";
            SetInputPrompt($"--- Player 1の推測ターン！ ({playerGuessCount + 1}回目) ---");
        }
        else
        {
            CPUInputText.text += "\nエラー: 可能な推測がなくなりました。プレイヤーの判定が間違っているか、ロジックエラーです。";
            gameMode = "GameOver";
            SetInputPrompt("ゲームオーバー: CPUロジックエラー");
        }
    }

    /// <summary>CPUが次の推測を行い、CPUInputTextに履歴として表示します。</summary>
    private void CPU_Guess()
    {
        cpuGuessCount++;

        int nextGuess;

        // ★★★ しらみつぶしモードへの移行判定 ★★★
        if (availableDigits.Count <= 4 && !useCombinatorialGuessing)
        {
            useCombinatorialGuessing = true;

            // possibleGuessesを、availableDigitsの組み合わせのみで構成される数字に絞り込む
            possibleGuesses = possibleGuesses
                .Where(p => IsComposedOfDigits(p, availableDigits))
                .ToList();

            CPUInputText.text += $"\n(AIモード変更: 残り{availableDigits.Count}桁。しらみつぶしモードへ移行)";
        }

        // リストの最初の数字を推測として採用
        nextGuess = possibleGuesses.FirstOrDefault();

        if (possibleGuesses.Count == 0)
        {
            lastCPUGuess = 0;
            return;
        }

        lastCPUGuess = nextGuess;

        // ★★★ CPUの推測内容をCPUInputTextに追記 ★★★
        CPUInputText.text += $"\n試行 {cpuGuessCount}: **{lastCPUGuess:D3}** を推測しました。";
    }

    // --- 新規 AI ロジック ---

    /// <summary>0H0Bだった場合、推測に使用した数字をavailableDigitsから除外します。</summary>
    private void FilterAvailableDigits(int guess, int hit, int blow)
    {
        if (hit == 0 && blow == 0)
        {
            int[] guessedDigits = new int[3];
            SetDigitsFromArray(guess, guessedDigits);

            // 0H0Bだった3桁の数字をavailableDigitsから削除
            availableDigits.Remove(guessedDigits[0]);
            availableDigits.Remove(guessedDigits[1]);
            availableDigits.Remove(guessedDigits[2]);
        }
    }

    /// <summary>数字が与えられた桁のみで構成されているかチェック</summary>
    private bool IsComposedOfDigits(int number, List<int> allowedDigits)
    {
        int[] digits = new int[3];
        SetDigitsFromArray(number, digits);
        return allowedDigits.Contains(digits[0]) &&
               allowedDigits.Contains(digits[1]) &&
               allowedDigits.Contains(digits[2]);
    }


    // --- 汎用ロジック補助関数 ---

    /// <summary>
    /// 入力チェックを行い、有効な場合は指定された配列に各桁を格納します。
    /// </summary>
    private bool InputCheckAndSetBaseDigits(int num, int[] targetArray)
    {
        string errorPrompt = "";
        bool isValid = true;

        if (num < 0 || num > 999)
        {
            errorPrompt = $"エラー: 000から999までの3桁の数字を入力してください。";
            isValid = false;
        }
        else
        {
            SetDigitsFromArray(num, tempDigits);
            int d0 = tempDigits[0];
            int d1 = tempDigits[1];
            int d2 = tempDigits[2];

            if (d0 == d1 || d1 == d2 || d2 == d0)
            {
                errorPrompt = $"エラー: 数字は重複しないようにしてください。";
                isValid = false;
            }
        }

        if (!isValid)
        {
            SetInputPrompt($"{errorPrompt}\n{GetCurrentPromptMessage()}");
            return false;
        }

        targetArray[0] = tempDigits[0];
        targetArray[1] = tempDigits[1];
        targetArray[2] = tempDigits[2];
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

    // --- ゲーム終了ロジック ---

    private string GetCurrentPromptMessage()
    {
        if (gameMode == "Setup_P1") return "Player 1: 秘密の3桁の数字（重複なし）を入力し、Enterで決定してください。";
        if (gameMode == "PlayerTurn") return $"--- Player 1の推測ターン！ ---";
        if (gameMode == "CPUTurn") return $"--- CPUの推測結果を確認 ---";
        return "";
    }

    private void GameClear(int correctGuess)
    {
        gameMode = "GameOver";
        PlayerInputText.text += $"\n\n🎉 **Player 1 WIN! (Game Clear!!)** {correctGuess:D3} で正解です！\nあなたは**{playerGuessCount}回目**で正解しました！\n";
        CPUBaseText.text = $"CPUの秘密の数字: {cpuBaseNumber[0]}{cpuBaseNumber[1]}{cpuBaseNumber[2]}";
        CPUInputText.text += $"\n\n--- Player 1の勝利によりゲーム終了 ---";
        SetInputPrompt("ゲーム終了: Player 1 の勝利です！");
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("Title");
        }
    }

    private void EndGame(int correctGuess)
    {
        gameMode = "GameOver";
        CPUInputText.text += $"\n\n🎉 **CPU WIN!** 秘密の数字 {correctGuess:D3} を当てました！\nCPUは**{cpuGuessCount}回目**で正解しました！\n";
        PlayerBaseText.text = $"あなたの秘密の数字: {playerBaseNumber[0]}{playerBaseNumber[1]}{playerBaseNumber[2]}";
        PlayerInputText.text += "\n\n--- CPUの勝利によりゲーム終了 ---";
        SetInputPrompt("ゲーム終了: CPU の勝利です！");
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("Title");
        }
    }

    private void DrawGame()
    {
        gameMode = "GameOver";
        SetInputPrompt("DRAW!!\n制限回数を超えました!!");
        PlayerBaseText.text = $"あなたの秘密の数字: {playerBaseNumber[0]}{playerBaseNumber[1]}{playerBaseNumber[2]}";
        CPUBaseText.text = $"CPUの秘密の数字: {cpuBaseNumber[0]}{cpuBaseNumber[1]}{cpuBaseNumber[2]}";
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("Title");
        }
    }

}