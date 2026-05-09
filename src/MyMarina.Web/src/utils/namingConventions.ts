// Dock naming conventions
export type DockConvention =
  | { kind: 'Lettered'; prefix?: string; suffix?: string; startAt?: number; padZeros?: boolean }
  | { kind: 'Numbered'; prefix?: string; suffix?: string; startAt?: number; padZeros?: boolean }
  | { kind: 'Manual'; names: string[] };

// Slip naming conventions
export type SlipConvention =
  | { kind: 'PerDockReset';   separator?: string; prefix?: string; suffix?: string; startAt?: number; padZeros?: boolean }
  | { kind: 'PerDockGlobal';  separator?: string; prefix?: string; suffix?: string; startAt?: number; padZeros?: boolean }
  | { kind: 'Sequential';     prefix?: string; suffix?: string; startAt?: number; padZeros?: boolean }
  | { kind: 'Manual';         names: string[][] };

function letter(i: number): string {
  // 0→A, 25→Z, 26→AA, 27→AB …
  let result = '';
  let n = i;
  do {
    result = String.fromCharCode(65 + (n % 26)) + result;
    n = Math.floor(n / 26) - 1;
  } while (n >= 0);
  return result;
}

function pad(n: number, pad_: boolean, ref: number): string {
  if (!pad_) return String(n);
  return String(n).padStart(String(ref).length, '0');
}

export function generateDockNames(count: number, convention: DockConvention): string[] {
  if (convention.kind === 'Manual') {
    return convention.names.slice(0, count);
  }

  const start = convention.startAt ?? 1;
  const usePad = convention.padZeros ?? false;
  const prefix = convention.prefix ?? '';
  const suffix = convention.suffix ?? '';

  return Array.from({ length: count }, (_, i) => {
    const label =
      convention.kind === 'Lettered'
        ? letter(start - 1 + i)
        : pad(start + i, usePad, start + count - 1);
    return `${prefix}${label}${suffix}`;
  });
}

export interface GenerateSlipsInput {
  dockNames: string[];
  slipsPerDock: number | number[];
  convention: SlipConvention;
}

export interface GeneratedDock {
  name: string;
  slips: string[];
}

export function generateDockSlips({ dockNames, slipsPerDock, convention }: GenerateSlipsInput): GeneratedDock[] {
  if (convention.kind === 'Manual') {
    return dockNames.map((name, di) => ({
      name,
      slips: convention.names[di] ?? [],
    }));
  }

  const slipCounts = Array.isArray(slipsPerDock)
    ? slipsPerDock
    : dockNames.map(() => slipsPerDock);

  const prefix = 'prefix' in convention ? (convention.prefix ?? '') : '';
  const suffix = 'suffix' in convention ? (convention.suffix ?? '') : '';
  const startAt = 'startAt' in convention ? (convention.startAt ?? 1) : 1;
  const usePad = 'padZeros' in convention ? (convention.padZeros ?? false) : false;
  const sep = 'separator' in convention ? (convention.separator ?? '-') : '-';

  const totalSlips = slipCounts.reduce((a, b) => a + b, 0);
  let globalCounter = startAt;

  return dockNames.map((dockName, di) => {
    const count = slipCounts[di] ?? 0;
    const slips: string[] = [];

    for (let i = 0; i < count; i++) {
      let label: string;

      switch (convention.kind) {
        case 'PerDockReset': {
          const n = startAt + i;
          label = `${dockName}${sep}${pad(n, usePad, startAt + count - 1)}`;
          break;
        }
        case 'PerDockGlobal': {
          label = `${dockName}${sep}${pad(globalCounter, usePad, startAt + totalSlips - 1)}`;
          globalCounter++;
          break;
        }
        case 'Sequential':
          label = `${prefix}${pad(globalCounter, usePad, startAt + totalSlips - 1)}${suffix}`;
          globalCounter++;
          break;
      }

      slips.push(label!);
    }

    return { name: dockName, slips };
  });
}
