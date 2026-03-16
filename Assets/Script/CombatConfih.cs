using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘システムの設定ファイル（ScriptableObject）
/// 
/// 目的：
/// - 全ての攻撃・ノックバック関連のパラメータを一元管理
/// - Inspector から値を調整可能にして、ゲームバランス調整を容易に
/// - 複数のスクリプト間での設定値の重複を排除
/// - マジックナンバー（ハードコードされた数値）を削除
/// 
/// 使用方法：
/// 1. Assets フォルダ > 右クリック > Create > Combat > Combat Config
/// 2. 作成されたアセットを Inspector で編集
/// 3. 各 GameObject の PlayerCombat / EnemyMovement に割り当て
/// </summary>
[CreateAssetMenu(fileName = "CombatConfig", menuName = "Combat/Combat Config")]
public class CombatConfig : ScriptableObject
{
    // ========== 攻撃ダメージ設定 ==========
    [Header("攻撃ダメージ")]
    public int jabDamage = 1;
    public int punchDamage = 2;
    public int kickDamage = 3;

    // ========== コンボシステム設定 ==========
    [Header("コンボシステム")]
    [Tooltip("プレイヤーが次の攻撃を入力できる時間（秒）")]
    public float comboWindowTime = 1f;

    [Range(0f, 0.3f)]
    [Tooltip("次のコンボ入力を受け付ける、アニメーション終了までの余裕時間（秒）\n値が小さいほど、タイミングが厳しくなります")]
    public float comboInputThreshold = 0.15f;

    // ========== ノックバック設定 ==========
    [Header("ノックバック")]
    [Tooltip("ノックバック時に敵に加える力の大きさ")]
    public float knockbackForce = 50f;

    [Tooltip("ノックバック状態の継続時間（敵が飛ぶ時間）")]
    public float knockbackDuration = 0.15f;

    [Tooltip("スタン状態の継続時間（敵が停止する時間）")]
    public float stunDuration = 0.3f;

    [Tooltip("ノックバックが有効な最大距離（-1 で無制限）\n敵が離れすぎている場合はノックバック無効")]
    public float minKnockbackDistance = -1f;

    // ========== 攻撃範囲設定 ==========
    [Header("攻撃範囲")]
    [Tooltip("プレイヤーの攻撃判定の半径")]
    public float attackRange = 1f;

    [Tooltip("攻撃後のクールタイム（秒）")]
    public float attackCooldown = 0f;

    // ========== 敵設定 ==========
    [Header("敵の動作設定")]
    [Tooltip("敵の攻撃のクールタイム（秒）")]
    public float enemyAttackCooldown = 2f;

    [Tooltip("敵がプレイヤーを検出できる範囲")]
    public float enemyDetectRange = 5f;

    [Tooltip("敵が攻撃できるプレイヤーとの距離")]
    public float enemyAttackRange = 2f;

    /// <summary>
    /// 攻撃の種類に応じて、対応するダメージを返す
    /// 
    /// 利点：
    /// - ダメージ値の決定ロジックが一箇所に集約
    /// - 攻撃タイプとダメージの対応が一目瞭然
    /// - switch 式（C# 8.0）で簡潔に実装
    /// </summary>
    public int GetDamageForAttack(PlayerCombat.AttackType attackType)
    {
        return attackType switch
        {
            PlayerCombat.AttackType.Jab => jabDamage,
            PlayerCombat.AttackType.Punch => punchDamage,
            PlayerCombat.AttackType.Kick => kickDamage,
            _ => jabDamage  // デフォルト値
        };
    }
}