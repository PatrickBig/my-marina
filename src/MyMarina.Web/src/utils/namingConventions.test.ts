import { describe, it, expect } from 'vitest';
import { generateDockNames, generateDockSlips, type DockConvention, type SlipConvention } from './namingConventions';

// ── generateDockNames ─────────────────────────────────────────────────────────

describe('generateDockNames – Lettered', () => {
  it('generates A–C for count=3', () => {
    const c: DockConvention = { kind: 'Lettered' };
    expect(generateDockNames(3, c)).toEqual(['A', 'B', 'C']);
  });

  it('generates 26 single letters A–Z', () => {
    const names = generateDockNames(26, { kind: 'Lettered' });
    expect(names).toHaveLength(26);
    expect(names[0]).toBe('A');
    expect(names[25]).toBe('Z');
  });

  it('generates AA after Z for count=27', () => {
    const names = generateDockNames(27, { kind: 'Lettered' });
    expect(names[26]).toBe('AA');
  });

  it('generates AB, AC after AA', () => {
    const names = generateDockNames(29, { kind: 'Lettered' });
    expect(names[26]).toBe('AA');
    expect(names[27]).toBe('AB');
    expect(names[28]).toBe('AC');
  });

  it('applies prefix and suffix', () => {
    const names = generateDockNames(3, { kind: 'Lettered', prefix: 'Dock ', suffix: ' Side' });
    expect(names).toEqual(['Dock A Side', 'Dock B Side', 'Dock C Side']);
  });

  it('respects startAt', () => {
    const names = generateDockNames(3, { kind: 'Lettered', startAt: 3 });
    expect(names).toEqual(['C', 'D', 'E']);
  });

  it('returns empty array for count=0', () => {
    expect(generateDockNames(0, { kind: 'Lettered' })).toEqual([]);
  });
});

describe('generateDockNames – Numbered', () => {
  it('generates 1, 2, 3 by default', () => {
    expect(generateDockNames(3, { kind: 'Numbered' })).toEqual(['1', '2', '3']);
  });

  it('pads zeros relative to last index', () => {
    const names = generateDockNames(10, { kind: 'Numbered', padZeros: true });
    expect(names[0]).toBe('01');
    expect(names[9]).toBe('10');
  });

  it('pads zeros for 100+ count', () => {
    const names = generateDockNames(100, { kind: 'Numbered', padZeros: true });
    expect(names[0]).toBe('001');
    expect(names[99]).toBe('100');
  });

  it('respects startAt', () => {
    expect(generateDockNames(3, { kind: 'Numbered', startAt: 5 })).toEqual(['5', '6', '7']);
  });

  it('applies prefix and suffix', () => {
    const names = generateDockNames(2, { kind: 'Numbered', prefix: 'D', suffix: '' });
    expect(names).toEqual(['D1', 'D2']);
  });
});

describe('generateDockNames – Manual', () => {
  it('returns provided names up to count', () => {
    const c: DockConvention = { kind: 'Manual', names: ['North', 'South', 'East'] };
    expect(generateDockNames(3, c)).toEqual(['North', 'South', 'East']);
  });

  it('truncates to count', () => {
    const c: DockConvention = { kind: 'Manual', names: ['A', 'B', 'C', 'D'] };
    expect(generateDockNames(2, c)).toEqual(['A', 'B']);
  });

  it('returns empty array for count=0', () => {
    const c: DockConvention = { kind: 'Manual', names: ['A'] };
    expect(generateDockNames(0, c)).toEqual([]);
  });
});

// ── generateDockSlips ─────────────────────────────────────────────────────────

describe('generateDockSlips – PerDockReset', () => {
  const conv: SlipConvention = { kind: 'PerDockReset' };

  it('resets counter per dock', () => {
    const result = generateDockSlips({ dockNames: ['A', 'B'], slipsPerDock: 3, convention: conv });
    expect(result[0].slips).toEqual(['A-1', 'A-2', 'A-3']);
    expect(result[1].slips).toEqual(['B-1', 'B-2', 'B-3']);
  });

  it('uses custom separator', () => {
    const c: SlipConvention = { kind: 'PerDockReset', separator: '.' };
    const result = generateDockSlips({ dockNames: ['A'], slipsPerDock: 2, convention: c });
    expect(result[0].slips).toEqual(['A.1', 'A.2']);
  });

  it('pads zeros within dock count', () => {
    const c: SlipConvention = { kind: 'PerDockReset', padZeros: true };
    const result = generateDockSlips({ dockNames: ['A'], slipsPerDock: 10, convention: c });
    expect(result[0].slips[0]).toBe('A-01');
    expect(result[0].slips[9]).toBe('A-10');
  });

  it('handles 0 slips per dock', () => {
    const result = generateDockSlips({ dockNames: ['A', 'B'], slipsPerDock: 0, convention: conv });
    expect(result[0].slips).toEqual([]);
    expect(result[1].slips).toEqual([]);
  });
});

describe('generateDockSlips – PerDockGlobal', () => {
  const conv: SlipConvention = { kind: 'PerDockGlobal' };

  it('continues counter across docks', () => {
    const result = generateDockSlips({ dockNames: ['A', 'B'], slipsPerDock: 3, convention: conv });
    expect(result[0].slips).toEqual(['A-1', 'A-2', 'A-3']);
    expect(result[1].slips).toEqual(['B-4', 'B-5', 'B-6']);
  });

  it('handles variable slips per dock', () => {
    const result = generateDockSlips({ dockNames: ['A', 'B'], slipsPerDock: [2, 4], convention: conv });
    expect(result[0].slips).toEqual(['A-1', 'A-2']);
    expect(result[1].slips).toEqual(['B-3', 'B-4', 'B-5', 'B-6']);
  });
});

describe('generateDockSlips – Sequential', () => {
  const conv: SlipConvention = { kind: 'Sequential' };

  it('generates sequential numbers across all docks', () => {
    const result = generateDockSlips({ dockNames: ['A', 'B'], slipsPerDock: 3, convention: conv });
    expect(result[0].slips).toEqual(['1', '2', '3']);
    expect(result[1].slips).toEqual(['4', '5', '6']);
  });

  it('applies prefix and suffix', () => {
    const c: SlipConvention = { kind: 'Sequential', prefix: 'S-', suffix: '' };
    const result = generateDockSlips({ dockNames: ['A'], slipsPerDock: 2, convention: c });
    expect(result[0].slips).toEqual(['S-1', 'S-2']);
  });

  it('pads zeros across total slip count', () => {
    const c: SlipConvention = { kind: 'Sequential', padZeros: true, startAt: 1 };
    const result = generateDockSlips({ dockNames: ['A', 'B'], slipsPerDock: 5, convention: c });
    expect(result[0].slips[0]).toBe('01');
    expect(result[1].slips[4]).toBe('10');
  });
});

describe('generateDockSlips – Manual', () => {
  it('uses provided slip names per dock', () => {
    const conv: SlipConvention = { kind: 'Manual', names: [['N1', 'N2'], ['S1']] };
    const result = generateDockSlips({ dockNames: ['North', 'South'], slipsPerDock: 0, convention: conv });
    expect(result[0].slips).toEqual(['N1', 'N2']);
    expect(result[1].slips).toEqual(['S1']);
  });

  it('defaults to empty array for missing dock index', () => {
    const conv: SlipConvention = { kind: 'Manual', names: [['A-1']] };
    const result = generateDockSlips({ dockNames: ['A', 'B'], slipsPerDock: 0, convention: conv });
    expect(result[1].slips).toEqual([]);
  });
});

describe('generateDockSlips – dock names preserved', () => {
  it('preserves dock name in result', () => {
    const result = generateDockSlips({
      dockNames: ['Marina North', 'Marina South'],
      slipsPerDock: 1,
      convention: { kind: 'PerDockReset' },
    });
    expect(result[0].name).toBe('Marina North');
    expect(result[1].name).toBe('Marina South');
  });
});
