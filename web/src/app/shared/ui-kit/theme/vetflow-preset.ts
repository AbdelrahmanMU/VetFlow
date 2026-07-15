import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

/**
 * The VetFlow theme preset — the single place the design language's color and
 * shape decisions are applied to the component foundation (ADR-0009,
 * ADR-0012). Feature code never sees PrimeNG or this preset.
 */
export const vetflowPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#f0fdfa',
      100: '#ccfbf1',
      200: '#99f6e4',
      300: '#5eead4',
      400: '#2dd4bf',
      500: '#14b8a6',
      600: '#0d9488',
      700: '#0f766e',
      800: '#115e59',
      900: '#134e4a',
      950: '#042f2e',
    },
    colorScheme: {
      light: {
        surface: {
          0: '#ffffff',
          50: '#fafaf9',
          100: '#f5f5f4',
          200: '#e7e5e4',
          300: '#d6d3d1',
          400: '#a8a29e',
          500: '#78716c',
          600: '#57534e',
          700: '#44403c',
          800: '#292524',
          900: '#1c1917',
          950: '#0c0a09',
        },
        primary: {
          color: '#0f766e',
          contrastColor: '#ffffff',
          hoverColor: '#115e59',
          activeColor: '#134e4a',
        },
      },
    },
  },
  components: {
    datatable: {
      colorScheme: {
        light: {
          headerCell: {
            background: '#fafaf9',
            color: '#57534e',
          },
          row: {
            background: '#ffffff',
            hoverBackground: '#fafaf9',
          },
        },
      },
    },
  },
});
