import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getPricingRules, getPricingRule, previewPrice,
  createPricingRule, updatePricingRule, deletePricingRule,
  getSlipPriceAdjustments, createSlipPriceAdjustment,
  updateSlipPriceAdjustment, deleteSlipPriceAdjustment,
  type PricingRulePredicateDto,
} from '@/api/api';

// ── Query keys ────────────────────────────────────────────────────────────────

export const pricingKeys = {
  rules: (marinaId: string) => ['pricing-rules', marinaId] as const,
  rule: (marinaId: string, ruleId: string) => ['pricing-rule', marinaId, ruleId] as const,
  preview: (marinaId: string, slipId: string, listingKind: string) =>
    ['pricing-preview', marinaId, slipId, listingKind] as const,
  adjustments: (slipId: string) => ['slip-adjustments', slipId] as const,
};

// ── Pricing rules queries ─────────────────────────────────────────────────────

export function usePricingRules(marinaId: string) {
  return useQuery({
    queryKey: pricingKeys.rules(marinaId),
    queryFn: () => getPricingRules(marinaId),
    enabled: !!marinaId,
  });
}

export function usePricingRule(marinaId: string, ruleId: string) {
  return useQuery({
    queryKey: pricingKeys.rule(marinaId, ruleId),
    queryFn: () => getPricingRule(marinaId, ruleId),
    enabled: !!marinaId && !!ruleId,
  });
}

export function usePricePreview(marinaId: string, slipId: string, listingKind: string, asOf?: string) {
  return useQuery({
    queryKey: pricingKeys.preview(marinaId, slipId, listingKind),
    queryFn: () => previewPrice(marinaId, slipId, listingKind, asOf),
    enabled: !!marinaId && !!slipId && !!listingKind,
  });
}

// ── Pricing rule mutations ────────────────────────────────────────────────────

export function useCreatePricingRule(marinaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      name: string;
      contributionKind: string;
      priority: number;
      predicate: PricingRulePredicateDto;
      rateKind: string;
      amount: number;
      minCharge?: number | null;
      effectiveFrom: string;
      effectiveTo?: string | null;
    }) => createPricingRule(marinaId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: pricingKeys.rules(marinaId) });
    },
  });
}

export function useUpdatePricingRule(marinaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ ruleId, data }: { ruleId: string; data: Parameters<typeof updatePricingRule>[2] }) =>
      updatePricingRule(marinaId, ruleId, data),
    onSuccess: (_, { ruleId }) => {
      qc.invalidateQueries({ queryKey: pricingKeys.rules(marinaId) });
      qc.invalidateQueries({ queryKey: pricingKeys.rule(marinaId, ruleId) });
    },
  });
}

export function useDeletePricingRule(marinaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ruleId: string) => deletePricingRule(marinaId, ruleId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: pricingKeys.rules(marinaId) });
    },
  });
}

// ── Slip price adjustment queries/mutations ───────────────────────────────────

export function useSlipPriceAdjustments(slipId: string, marinaId: string) {
  return useQuery({
    queryKey: pricingKeys.adjustments(slipId),
    queryFn: () => getSlipPriceAdjustments(slipId, marinaId),
    enabled: !!slipId && !!marinaId,
  });
}

export function useCreateSlipPriceAdjustment(slipId: string, marinaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { listingKind: string; label: string; amount: number }) =>
      createSlipPriceAdjustment(slipId, marinaId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: pricingKeys.adjustments(slipId) });
    },
  });
}

export function useUpdateSlipPriceAdjustment(slipId: string, marinaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ adjustmentId, data }: { adjustmentId: string; data: { label?: string; amount?: number } }) =>
      updateSlipPriceAdjustment(slipId, adjustmentId, marinaId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: pricingKeys.adjustments(slipId) });
    },
  });
}

export function useDeleteSlipPriceAdjustment(slipId: string, marinaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (adjustmentId: string) => deleteSlipPriceAdjustment(slipId, adjustmentId, marinaId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: pricingKeys.adjustments(slipId) });
    },
  });
}
