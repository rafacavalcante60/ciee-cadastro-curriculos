import { TelefonePipe } from './telefone.pipe';

describe('TelefonePipe', () => {
  const pipe = new TelefonePipe();

  it('formata celular com onze dígitos', () => {
    expect(pipe.transform('11912345678')).toBe('(11) 91234-5678');
  });

  it('formata fixo com dez dígitos', () => {
    expect(pipe.transform('1134567890')).toBe('(11) 3456-7890');
  });

  it('devolve sem alterar o que não tem dez ou onze dígitos', () => {
    expect(pipe.transform('123')).toBe('123');
  });

  it('mantém nulo', () => {
    expect(pipe.transform(null)).toBeNull();
  });
});
