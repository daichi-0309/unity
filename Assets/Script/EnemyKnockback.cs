using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 改善版：敵のノックバック処理
/// 
/// 主な改善点：
/// 1. ノックバック距離チェックが動的に設定可能
///    - 以前：距離 2f で固定
///    - 改善：CombatConfig で距離を管理（-1 で無制限）
/// 
/// 2. Coroutine の多重実行を防止
///    - 同じ敵がノックバック中に再度ヒットした場合、
///      前のコルーチンを停止して新しいものを開始
/// 
/// 3. コルーチン参照を保持して管理
///    - StopCoroutine を確実に呼び出すため
/// </summary>
public class EnemyKnockback : MonoBehaviour
{
    // ========== 参照 ==========
    private Rigidbody2D rb;                        // 敵の物理演算コンポーネント
    private EnemyMovement enemyMovement;           // 敵の状態管理コンポーネント
    private Coroutine stunCoroutine;               // 現在実行中のスタンコルーチン参照

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyMovement = GetComponent<EnemyMovement>();

        // 参照の null チェック
        if (rb == null)
            Debug.LogError($"[EnemyKnockback] {gameObject.name} に Rigidbody2D がありません");

        if (enemyMovement == null)
            Debug.LogError($"[EnemyKnockback] {gameObject.name} に EnemyMovement がありません");
    }

    /// <summary>
    /// ノックバック処理を実行する
    /// 呼び出し元：PlayerCombat の ProcessEnemyHit() メソッド
    /// 
    /// パラメータ：
    /// - playerTransform：攻撃者（プレイヤー）の位置
    /// - knockbackForce：ノックバック時の力の大きさ
    /// - knockbackTime：ノックバック状態の継続時間（敵が飛ぶ時間）
    /// - stunTime：スタン状態の継続時間（敵が停止する時間）
    /// - minDistance：ノックバック有効距離（-1 で無制限）
    /// 
    /// 戻り値：
    /// - true：ノックバック成功
    /// - false：距離チェックに引っかかってノックバック失敗
    /// </summary>
    public bool Knockback(Transform playerTransform, float knockbackForce,
                         float knockbackTime, float stunTime, float minDistance = -1f)
    {
        // プレイヤーの Transform が存在するか確認
        if (playerTransform == null)
        {
            Debug.LogError("[EnemyKnockback] playerTransform が null です");
            return false;
        }

        // ========== 距離チェック（設定されている場合） ==========
        // minDistance = -1 の場合は距離チェック無し
        if (minDistance > 0)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            if (distance > minDistance)
            {
                Debug.Log($"[ノックバック] {gameObject.name} が遠すぎます（距離: {distance:F2}m > {minDistance}m）");
                return false;
            }
        }

        // ========== 既存のコルーチンを停止 ==========
        // 同じ敵がノックバック中に再度攻撃を受けた場合、前のコルーチンを停止
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        // ========== 敵の状態を Knockback に変更 ==========
        enemyMovement.ChangeState(EnemyState.Knockback);

        // ========== ノックバック方向を計算 ==========
        // プレイヤーから敵が離れる方向
        Vector2 direction = (transform.position - playerTransform.position).normalized;

        // ========== Rigidbody2D に速度を設定 ==========
        rb.linearVelocity = direction * knockbackForce;

        // ========== ノックバック＆スタン時間の管理コルーチンを開始 ==========
        stunCoroutine = StartCoroutine(StunTimer(knockbackTime, stunTime));

        Debug.Log($"[ノックバック] {gameObject.name} にノックバックを適用しました");
        return true;
    }

    /// <summary>
    /// ノックバック＆スタン時間を管理するコルーチン
    /// 
    /// フロー：
    /// 1. knockbackTime 秒間 → 敵が飛ぶ（速度が有効）
    /// 2. 速度をリセット
    /// 3. stunTime 秒間 → 敵が停止（スタン状態）
    /// 4. stunTime が終了 → 敵が Idle 状態に戻る
    /// 
    /// パラメータ：
    /// - knockbackTime：ノックバック状態の継続時間（敵が飛ぶ時間）
    /// - stunTime：スタン状態の継続時間（敵が停止する時間）
    /// </summary>
    IEnumerator StunTimer(float knockbackTime, float stunTime)
    {
        // ========== Phase 1：ノックバック（敵が飛ぶ） ==========
        // knockbackTime 秒間、Rigidbody2D の速度が有効
        yield return new WaitForSeconds(knockbackTime);

        // ========== Phase 1 終了：速度をリセット ==========
        // これ以降、敵は飛ばずに停止状態になる
        rb.linearVelocity = Vector2.zero;

        // ========== Phase 2：スタン（敵が停止） ==========
        // stunTime 秒間、敵は動かない状態を継続
        yield return new WaitForSeconds(stunTime);

        // ========== Phase 2 終了：状態を Idle に戻す ==========
        // これにより、敵は再度プレイヤーを追跡または攻撃できるようになる
        enemyMovement.ChangeState(EnemyState.Idle);
        stunCoroutine = null;  // コルーチン参照をリセット
    }
}