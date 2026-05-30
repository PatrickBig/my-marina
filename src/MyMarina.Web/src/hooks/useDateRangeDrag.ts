import { useState, useCallback } from 'react';
import { toast } from 'sonner';
import type { AvailabilityWindowDto } from '@/api/api';

export interface DateRange {
  start: Date;
  end: Date;
}

function overlaps(a: DateRange, windows: AvailabilityWindowDto[]): boolean {
  return windows.some((w) => {
    const wStart = new Date(w.startsAt);
    const wEnd = new Date(w.endsAt);
    return a.start <= wEnd && a.end >= wStart;
  });
}

export function useDateRangeDrag(existingWindows: AvailabilityWindowDto[]) {
  const [dragging, setDragging] = useState(false);
  const [dragStart, setDragStart] = useState<Date | null>(null);
  const [dragEnd, setDragEnd] = useState<Date | null>(null);

  const onPointerDown = useCallback((date: Date) => {
    setDragging(true);
    setDragStart(date);
    setDragEnd(date);
  }, []);

  const onPointerEnter = useCallback((date: Date) => {
    if (!dragging) return;
    setDragEnd(date);
  }, [dragging]);

  const onPointerUp = useCallback((): DateRange | null => {
    if (!dragging || !dragStart || !dragEnd) {
      setDragging(false);
      return null;
    }
    setDragging(false);
    const start = dragStart <= dragEnd ? dragStart : dragEnd;
    const end = dragStart <= dragEnd ? dragEnd : dragStart;
    const range: DateRange = { start, end };
    if (overlaps(range, existingWindows)) {
      toast.error('Selected dates overlap an existing availability window.');
      setDragStart(null);
      setDragEnd(null);
      return null;
    }
    setDragStart(null);
    setDragEnd(null);
    return range;
  }, [dragging, dragStart, dragEnd, existingWindows]);

  const dragRange: DateRange | null = dragStart && dragEnd
    ? {
        start: dragStart <= dragEnd ? dragStart : dragEnd,
        end: dragStart <= dragEnd ? dragEnd : dragStart,
      }
    : null;

  return { dragging, dragRange, onPointerDown, onPointerEnter, onPointerUp };
}
