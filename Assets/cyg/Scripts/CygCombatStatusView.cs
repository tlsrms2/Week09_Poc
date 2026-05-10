using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cyg.UI
{
    [DisallowMultipleComponent]
    public sealed class CygCombatStatusView : MonoBehaviour
    {
        private enum HpTextMode
        {
            CurrentOnly,
            CurrentSlashMax
        }

        [Header("Optional Runtime Sources")]
        [SerializeField] private CombatUnit playerUnit;
        [SerializeField] private CombatUnit enemyUnit;
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private bool findUnitsOnEnable = true;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private TextMeshProUGUI enemyHpText;
        [SerializeField] private TextMeshProUGUI playerDefenseText;
        [SerializeField] private TextMeshProUGUI playerAttackText;
        [SerializeField] private TextMeshProUGUI enemyAttackText;
        [SerializeField] private HpTextMode hpTextMode = HpTextMode.CurrentOnly;

        [Header("Optional Bars")]
        [SerializeField] private Image playerHpFill;
        [SerializeField] private Image enemyHpFill;

        [Header("Fallback")]
        [SerializeField] private string missingValueText = "--";

        private int accumulatedDamage = 0;
        private bool isResolving = false;

        private void OnEnable()
        {
            GameEvents.OnPlayerHpChanged  += HandlePlayerHpChanged;
            GameEvents.OnEnemyHpChanged   += HandleEnemyHpChanged;
            GameEvents.OnPlayerDefenseChanged += HandlePlayerDefenseChanged;
            //GameEvents.OnBlockPlaced            += HandleBlockPlaced;
            GameEvents.OnDrawPhaseStarted       += HandleDrawPhaseStarted;
            GameEvents.OnResolutionPhaseStarted += HandleResolutionPhaseStarted;
            GameEvents.OnResolutionResult       += HandleResolutionResult;
            GameEvents.OnResolutionComplete     += HandleResolutionComplete;

            if (findUnitsOnEnable)
                RefreshUnitReferences();

            RefreshSnapshot();
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHpChanged  -= HandlePlayerHpChanged;
            GameEvents.OnEnemyHpChanged   -= HandleEnemyHpChanged;
            GameEvents.OnPlayerDefenseChanged -= HandlePlayerDefenseChanged;
            GameEvents.OnBlockPlaced            -= HandleBlockPlaced;
            GameEvents.OnDrawPhaseStarted       -= HandleDrawPhaseStarted;
            GameEvents.OnResolutionPhaseStarted -= HandleResolutionPhaseStarted;
            GameEvents.OnResolutionResult       -= HandleResolutionResult;
            GameEvents.OnResolutionComplete     -= HandleResolutionComplete;
        }

        public void RefreshUnitReferences()
        {
            CombatUnit[] units = FindObjectsByType<CombatUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i].IsPlayer) playerUnit = units[i];
                else                   enemyUnit  = units[i];
            }

            if (combatManager == null)
                combatManager = FindAnyObjectByType<CombatManager>();
            if (gridManager == null)
                gridManager = FindAnyObjectByType<GridManager>();
        }

        public void RefreshSnapshot()
        {
            if (playerUnit != null)
            {
                HandlePlayerHpChanged(playerUnit.CurrentHp, playerUnit.MaxHp);
                HandlePlayerDefenseChanged(playerUnit.Defense);
            }
            else
            {
                SetText(playerHpText, missingValueText);
                SetFill(playerHpFill, 0f);
                SetText(playerDefenseText, missingValueText);
            }

            if (enemyUnit != null)
                HandleEnemyHpChanged(enemyUnit.CurrentHp, enemyUnit.MaxHp);
            else
            {
                SetText(enemyHpText, missingValueText);
                SetFill(enemyHpFill, 0f);
            }

            RefreshAttackTexts();
        }

        private void RefreshAttackTexts()
        {
            int playerAtk = isResolving
                ? accumulatedDamage
                : (gridManager != null ? gridManager.GetPreview().damage : 0);
            SetText(playerAttackText, playerAtk.ToString());

            string enemyAtk = combatManager != null
                ? combatManager.EnemyBaseDamage.ToString()
                : missingValueText;
            SetText(enemyAttackText, enemyAtk);
        }

        private void HandleBlockPlaced(CardData _, int __, int ___)
        {
            RefreshAttackTexts();
        }

        private void HandleDrawPhaseStarted(int _)
        {
            isResolving = false;
            accumulatedDamage = 0;
            RefreshAttackTexts();
        }

        private void HandleResolutionPhaseStarted()
        {
            isResolving = true;
            accumulatedDamage = 0;
            RefreshAttackTexts();
        }

        private void HandleResolutionResult(ResolutionResult result)
        {
            accumulatedDamage += result.damage;
            SetText(playerAttackText, accumulatedDamage.ToString());
        }

        private void HandleResolutionComplete()
        {
            isResolving = false;
        }

        private void HandlePlayerHpChanged(int current, int max)
        {
            SetHpText(playerHpText, current, max);
            SetHpFill(playerHpFill, current, max);
        }

        private void HandleEnemyHpChanged(int current, int max)
        {
            SetHpText(enemyHpText, current, max);
            SetHpFill(enemyHpFill, current, max);
        }

        private void HandlePlayerDefenseChanged(int defense)
        {
            SetText(playerDefenseText, defense.ToString());
        }

        private void SetHpText(TextMeshProUGUI target, int current, int max)
        {
            if (target == null)
            {
                return;
            }

            string text = hpTextMode == HpTextMode.CurrentSlashMax
                ? $"{current}/{max}"
                : current.ToString();

            target.SetText(text);
        }

        private static void SetText(TextMeshProUGUI target, string text)
        {
            if (target != null)
            {
                target.SetText(text);
            }
        }

        private static void SetHpFill(Image target, int current, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
            SetFill(target, ratio);
        }

        private static void SetFill(Image target, float value)
        {
            if (target != null)
            {
                target.fillAmount = value;
            }
        }
    }
}
