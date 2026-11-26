ALTER TABLE sisplan.poa
    ADD COLUMN presupuesto_referencia numeric(14, 2)
        CHECK (presupuesto_referencia >= 0);
