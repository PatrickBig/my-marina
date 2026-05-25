import { describe, it, expect } from 'vitest';
import { resolvePrice, type PriceResolverPlan } from './priceResolver';

const noAddOns: PriceResolverPlan['amenityAddOns'] = [];

function plan(overrides: Partial<PriceResolverPlan> = {}): PriceResolverPlan {
  return {
    transientRateKind: null, transientAmount: null, transientMinCharge: null,
    leaseRateKind: null, leaseAmount: null, leaseMinCharge: null,
    amenityAddOns: noAddOns,
    ...overrides,
  };
}

describe('resolvePrice', () => {
  describe('null rate kind → null', () => {
    it('transient null when rateKind absent', () => {
      const { transient } = resolvePrice(plan(), 40, 14, []);
      expect(transient).toBeNull();
    });

    it('lease null when rateKind absent', () => {
      const { lease } = resolvePrice(plan(), 40, 14, []);
      expect(lease).toBeNull();
    });
  });

  describe('Flat rate', () => {
    it('returns fixed amount regardless of dimensions', () => {
      const { transient } = resolvePrice(plan({ transientRateKind: 'Flat', transientAmount: 120 }), 50, 20, []);
      expect(transient).toBe(120);
    });

    it('lease flat rate', () => {
      const { lease } = resolvePrice(plan({ leaseRateKind: 'Flat', leaseAmount: 2800 }), 40, 14, []);
      expect(lease).toBe(2800);
    });
  });

  describe('PerFoot rate', () => {
    it('multiplies amount by slip length', () => {
      const { transient } = resolvePrice(plan({ transientRateKind: 'PerFoot', transientAmount: 3.5 }), 40, 14, []);
      expect(transient).toBeCloseTo(140);
    });

    it('different lengths produce different results', () => {
      const p = plan({ transientRateKind: 'PerFoot', transientAmount: 3.5 });
      expect(resolvePrice(p, 60, 14, []).transient).toBeCloseTo(210);
    });
  });

  describe('PerArea rate', () => {
    it('multiplies amount by length × beam', () => {
      const { transient } = resolvePrice(plan({ transientRateKind: 'PerArea', transientAmount: 0.18 }), 40, 14, []);
      expect(transient).toBeCloseTo(0.18 * 40 * 14);
    });

    it('beam changes the result', () => {
      const p = plan({ transientRateKind: 'PerArea', transientAmount: 0.18 });
      const narrow = resolvePrice(p, 40, 10, []).transient!;
      const wide   = resolvePrice(p, 40, 20, []).transient!;
      expect(wide).toBeGreaterThan(narrow);
    });
  });

  describe('MinCharge floor', () => {
    it('applies MinCharge when base rate is below it', () => {
      const p = plan({ transientRateKind: 'PerFoot', transientAmount: 1, transientMinCharge: 75 });
      expect(resolvePrice(p, 40, 14, []).transient).toBe(75);
    });

    it('does not apply MinCharge when base exceeds it', () => {
      const p = plan({ transientRateKind: 'PerFoot', transientAmount: 3.5, transientMinCharge: 75 });
      expect(resolvePrice(p, 40, 14, []).transient).toBeCloseTo(140);
    });
  });

  describe('Amenity add-ons', () => {
    const addOns: PriceResolverPlan['amenityAddOns'] = [
      { amenity: 'Electric50A', transientAmount: 15, leaseAmount: 120 },
      { amenity: 'Covered',     transientAmount: 20, leaseAmount: 150 },
    ];

    it('adds transient add-on when amenity present', () => {
      const p = plan({ transientRateKind: 'Flat', transientAmount: 100, amenityAddOns: addOns });
      expect(resolvePrice(p, 40, 14, ['Electric50A']).transient).toBe(115);
    });

    it('stacks multiple add-ons', () => {
      const p = plan({ transientRateKind: 'Flat', transientAmount: 100, amenityAddOns: addOns });
      expect(resolvePrice(p, 40, 14, ['Electric50A', 'Covered']).transient).toBe(135);
    });

    it('adds lease add-on', () => {
      const p = plan({ leaseRateKind: 'Flat', leaseAmount: 2800, amenityAddOns: addOns });
      expect(resolvePrice(p, 40, 14, ['Electric50A']).lease).toBe(2920);
    });

    it('skips add-on when amenity absent', () => {
      const p = plan({ transientRateKind: 'Flat', transientAmount: 100, amenityAddOns: addOns });
      expect(resolvePrice(p, 40, 14, []).transient).toBe(100);
    });

    it('add-on with null amount treated as zero', () => {
      const nullAddOns: PriceResolverPlan['amenityAddOns'] = [
        { amenity: 'Electric50A', transientAmount: null, leaseAmount: null },
      ];
      const p = plan({ transientRateKind: 'Flat', transientAmount: 100, amenityAddOns: nullAddOns });
      expect(resolvePrice(p, 40, 14, ['Electric50A']).transient).toBe(100);
    });
  });
});
