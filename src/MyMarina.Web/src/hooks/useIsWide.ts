import { useEffect, useState } from 'react';

export function useIsWide(breakpoint: number): boolean {
  const [wide, setWide] = useState(() => typeof window !== 'undefined' ? window.innerWidth >= breakpoint : false);

  useEffect(() => {
    function check() { setWide(window.innerWidth >= breakpoint); }
    window.addEventListener('resize', check);
    return () => window.removeEventListener('resize', check);
  }, [breakpoint]);

  return wide;
}
